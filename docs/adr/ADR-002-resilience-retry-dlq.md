# ADR-002: Отказоустойчивость Kafka-консьюмеров (Retry + DLQ)

| Поле | Значение |
|---|---|
| **Статус** | Accepted |
| **Дата** | 01.08.2026 |
| **Автор** | skilllab |
| **Затрагивает** | Events (Infrastructure) |
| **Связанные ADR** | ADR-001 (CQRS) |

---

## Контекст

`BookingRequestedConsumer` — критичный consumer в сервисе Events, обрабатывающий запросы на бронирование. При сбоях (конкурентные конфликты БД, временная недоступность) сообщение терялось, и бронь оставалась в `Pending` навсегда. Требовалось:

- не терять сообщения при временных сбоях БД;
- не застревать на «плохих» сообщениях (невалидный JSON);
- дать оператору возможность вручную разобрать необрабатываемые сообщения;
- не допустить бесконечных циклов повторной обработки.

---

## Решение

Реализована **трёхуровневая стратегия отказоустойчивости** для `BookingRequestedConsumer`:

```
booking-requested
    │
    ▼
┌─────────────────────────────┐
│ BookingRequestedConsumer    │  ← primary consumer
│  1. In-place retry (Polly)  │
│  2. Retry topic             │
│  3. Dead Letter Queue       │
└─────────────────────────────┘
    │                │                │
    ▼                ▼                ▼
 success     booking-requested-retry   booking-requested-dlq
                  │
                  ▼
         ┌──────────────────────────────┐
         │ BookingRequestedRetryConsumer │  ← retry consumer
         │  · backoff delay             │
         │  · max attempts check        │
         │  · DLQ on exhaustion         │
         └──────────────────────────────┘
```

### Уровень 1: In-place retry (Polly)

`BookingRequestedDbRetryPolicy` — обёртка над `ResiliencePipeline` из Polly, срабатывает при `DbUpdateException`:

| Параметр | Значение по умолчанию | Назначение |
|---|---|---|
| `InPlaceRetryCount` | 3 | Число быстрых повторов |
| `InPlaceRetryBaseDelayMs` | 200 | Базовая задержка (миллисекунды) |
| Backoff | Exponential | Удвоение задержки на каждом шаге |
| Jitter | ✅ | Случайный разброс для рассинхронизации повторов |

Эти повторы происходят **внутри одного цикла Consume**, без участия Kafka retry-топика. Быстро (миллисекунды), дёшево, подходит для transient-ошибок.

Если `InPlaceRetryCount` исчерпаны — `DbUpdateException` пробрасывается дальше.

### Уровень 2: Retry topic

После исчерпания in-place попыток, primary consumer (`BookingRequestedConsumer`) публикует `BookingRequestedRetryEnvelope` в топик `booking-requested-retry` через outbox:

```csharp
var retryEnvelope = new BookingRequestedRetryEnvelope(
    message,                           // исходный BookingRequested
    RetryAttempt: 1,                   // номер retry-попытки
    FirstFailedAtUtc: now,             // время первого сбоя
    NextAttemptAtUtc: now.Add(delay),  // когда можно повторить
    LastError: ex.Message,             // последняя ошибка
    RawPayload: rawPayload);           // сырые данные для DLQ
```

`BookingRequestedRetryConsumer` читает retry-топик:

1. Ждёт до `NextAttemptAtUtc` (защита от мгновенных повторных лавин).
2. Повторно вызывает `BookingRequestedMessageProcessor` через in-place retry (Polly).
3. Если успех — коммит offset.
4. Если снова `DbUpdateException`:
   - Проверяет `RetryAttempt >= RetryTopicMaxAttempts` (по умолчанию 5).
   - Если лимит не исчерпан — публикует следующий retry-конверт с увеличенным `RetryAttempt` и новым `NextAttemptAtUtc`.
   - Если лимит исчерпан — отправляет в DLQ.
5. Любое другое исключение — сразу DLQ.

Задержка retry вычисляется по формуле **exponential backoff с потолком**:

```
delay = min(
    RetryTopicInitialDelaySeconds × 2^(attempt-1),
    RetryTopicMaxDelaySeconds
)
```

| Параметр | Значение по умолчанию |
|---|---|
| `RetryTopicInitialDelaySeconds` | 30 |
| `RetryTopicMaxDelaySeconds` | 900 (15 минут) |
| `RetryTopicMaxAttempts` | 5 |

Пример задержек: 30с → 60с → 2м → 4м → 8м → 15м (с потолком).

### Уровень 3: Dead Letter Queue

Сообщения, которые невозможно обработать даже после всех retry-попыток, помещаются в топик `booking-requested-dlq`:

- **Невалидный JSON** → DLQ сразу, без ретраев.
- **Null-сообщение** → DLQ сразу, без ретраев.
- **Исчерпан лимит retry-попыток** → DLQ.
- **Не-DbUpdateException** → DLQ сразу.

Формат DLQ-сообщения (`BookingRequestedDlqMessage`):

| Поле | Описание |
|---|---|
| `DlqId` | Уникальный идентификатор DLQ-записи |
| `SourceTopic` | Откуда пришло (booking-requested / booking-requested-retry) |
| `RawPayload` | Оригинальное сырое сообщение |
| `Error` | Текст ошибки |
| `Timestamp` | Время попадания в DLQ |
| `RetryAttempt` | На какой попытке сдались |
| `OriginalMessageId` | MessageId исходного BookingRequested (если был) |

### Гарантии доставки

- **Все retry/dlq-сообщения проходят через outbox** — даже при временной недоступности Kafka сообщение не будет потеряно.
- **Commit offset только после принятия решения** (success/retry/dlq) — исключает потерю при падении consumer'а.
- **EnableAutoCommit = false** — ручной контроль смещений.
- **Идемпотентность через `ProcessedMessages`** — повторная доставка Kafka-сообщения не приводит к повторной обработке.

### Переиспользование процессора

Бизнес-логика обработки вынесена в `BookingRequestedMessageProcessor` и переиспользуется обоими consumer'ами (primary + retry). Это исключает дублирование кода и гарантирует одинаковое поведение.

---

## Альтернативы

| Вариант | Плюсы | Минусы | Решение |
|---|---|---|---|
| **Только Polly in-place** | Простота | Нет защиты от длительных сбоев; после N быстрых попыток сообщение теряется | ❌ Недостаточно |
| **Прямой retry без outbox** | Меньше кода | Риск потери сообщения при недоступности Kafka | ❌ Ненадёжно |
| **Внешний retry-оркестратор (Temporal/Cadence)** | Мощный workflow | Тяжёлая инфраструктура, overkill для микросервиса | ❌ Over-engineering |
| **Трёхуровневая: Polly + Retry topic + DLQ (через outbox)** | Гарантия доставки, настраиваемые задержки, ручной разбор «плохих» сообщений | Три топика, два consumer'а, сложнее в отладке | ✅ Выбран |

---

## Последствия

### Положительные

- Сообщения не теряются ни при каких сценариях (outbox гарантирует запись до публикации)
- Быстрые transient-сбои обрабатываются мгновенно (in-place Polly, миллисекунды)
- Длительные сбои обрабатываются с растущей задержкой (retry topic, exponential backoff)
- «Плохие» сообщения не блокируют обработку очереди и попадают в DLQ для ручного разбора
- Бизнес-логика не дублируется (общий `BookingRequestedMessageProcessor`)
- Все настройки вынесены в `KafkaOptions`, легко менять без перекомпиляции
- Трассировка сохраняется при переходе между топиками (trace context в Kafka headers)

### Отрицательные

- Три дополнительных топика (`retry`, `dlq`) увеличивают сложность инфраструктуры
- Retry-сообщения могут накапливаться при длительных сбоях (нужен мониторинг размера retry-топика)
- Exponential backoff означает, что максимальная задержка попытки — 15 минут; бронь может висеть в Pending до 30+ минут суммарно

### Риски

- **Лавина retry-сообщений**: при массовом сбое БД все сообщения уйдут в retry-топик. `NextAttemptAtUtc` и backoff снижают риск, но нужен алерт на рост `booking-requested-retry`.
- **Раздувание DLQ**: без автоматической очистки DLQ будет расти бесконечно. Необходим процесс ручного разбора или retention policy на топике.
- **Outbox как единая точка**: публикация retry/dlq через outbox гарантирует доставку, но добавляет задержку (outbox polling interval).

---

## Конфигурация

```json
{
  "KafkaOptions": {
    "InPlaceRetryCount": 3,
    "InPlaceRetryBaseDelayMs": 200,
    "RetryTopicMaxAttempts": 5,
    "RetryTopicInitialDelaySeconds": 30,
    "RetryTopicMaxDelaySeconds": 900
  }
}
```

---

## Топики

| Топик | Назначение | Кто пишет | Кто читает |
|---|---|---|---|
| `booking-requested` | Исходные запросы бронирования | Booking (outbox) | `BookingRequestedConsumer` (Events) |
| `booking-requested-retry` | Отложенные повторные попытки | `BookingRequestedConsumer`, `BookingRequestedRetryConsumer` (через outbox) | `BookingRequestedRetryConsumer` (Events, consumer group `-retry`) |
| `booking-requested-dlq` | «Мёртвые» сообщения | `BookingRequestedConsumer`, `BookingRequestedRetryConsumer` (через outbox) | Оператор / инструмент ручного разбора |

---

## Связанные файлы

- `EventForge.Events.Infrastructure/Services/BookingRequestedConsumer.cs` — primary consumer (in-place retry → retry topic / DLQ)
- `EventForge.Events.Infrastructure/Services/BookingRequestedRetryConsumer.cs` — retry consumer (backoff → retry / DLQ)
- `EventForge.Events.Infrastructure/Services/BookingRequestedDbRetryPolicy.cs` — Polly-политика in-place retry
- `EventForge.Events.Infrastructure/Services/BookingRequestedMessageProcessor.cs` — общая бизнес-логика
- `EventForge.Contract/Brokers/BookingRequestedRetryEnvelope.cs` — контракт retry-конверта
- `EventForge.Contract/Brokers/BookingRequestedDlqMessage.cs` — контракт DLQ-сообщения
- `EventForge.Events.Infrastructure/Entities/KafkaOptions.cs` — настройки retry
- `deploy/docker-compose.yml` — инициализация топиков (kafka-init-topics)

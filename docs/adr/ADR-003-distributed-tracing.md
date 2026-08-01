# ADR-003: Распределённая трассировка Kafka-сообщений (W3C Trace Context + OpenTelemetry)

| Поле | Значение |
|---|---|
| **Статус** | Accepted |
| **Дата** | 2025-07-04 |
| **Автор** | EventForge Team |
| **Затрагивает** | Booking (Infrastructure), Events (Infrastructure) |
| **Связанные ADR** | ADR-001 (CQRS), ADR-002 (Resilience Retry/DLQ) |

---

## Контекст

EventForge использует асинхронный обмен сообщениями через Kafka с паттерном Transactional Outbox. Полный цикл бронирования проходит через 6 Kafka-топиков и 3 сервиса, несколько границ сети, два типа БД (SQL Server/PostgreSQL) и Redis-кэш:

```
HTTP → BookingsController → Outbox → Kafka → Events → Outbox → Kafka → Bookings
```

При возникновении ошибок или задержек было невозможно определить, **на каком именно шаге** произошёл сбой и **сколько времени** занял каждый этап. Требовалось:

- видеть полный путь запроса от HTTP до финального consumer'а;
- определять узкие места (задержки outbox polling, время обработки в Events, время доставки Kafka);
- не терять трассировку при прохождении через Outbox (БД);
- не привязываться к проприетарным форматам.

---

## Решение

Внедрена **распределённая трассировка на основе W3C Trace Context** с использованием `System.Diagnostics.Activity` (OpenTelemetry SDK) и `KafkaTraceContext` как моста между HTTP/Activity и Kafka-заголовками.

### Ключевой компонент: `KafkaTraceContext`

`KafkaTraceContext` — `internal static` класс в каждом сервисе, реализующий три операции:

```
KafkaTraceContext
├── InjectCurrentContext(Headers)          → сериализация Activity.Current в заголовки Kafka
├── ExtractFromHeaders(Headers)            → десериализация из Kafka-заголовков
└── ExtractFromOutbox(traceParent, state)  → десериализация из полей OutboxMessage
```

Две копии класса (по одной на сервис) отличаются только именем `ActivitySource`:
- `EventForge.Events` → `new ActivitySource("EventForge.Events")`
- `EventForge.Booking` → `new ActivitySource("EventForge.Booking")`

### Стандарт: W3C Trace Context (версия 00)

В качестве wire-формата используется [W3C Trace Context Level 2](https://www.w3.org/TR/trace-context-2/) в версии `00`. Это открытый стандарт, совместимый с Jaeger, Zipkin, Grafana Tempo, AWS X-Ray, Google Cloud Trace.

Заголовки, передаваемые в Kafka-сообщениях:

| Заголовок Kafka | W3C-поле | Формат | Пример |
|---|---|---|---|
| `traceparent` | `traceparent` | `00-{traceId(32)}-{spanId(16)}-{flags(2)}` | `00-0af7...b19c-b7ad...0331-01` |
| `tracestate` | `tracestate` | `vendor1=value1,vendor2=value2` | `ot=abc123:def456` |

Формат `traceparent` детально:
```
00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
││ │                              │                  ││
││ └─ trace_id (32 hex, 16 байт)   └─ parent_id      └─ flags
│└─ version (00 = фиксированный)       (16 hex, 8 байт)  (01 = sampled)
└── hex-префикс
```

### Схема передачи контекста

#### 1. Producer: запись контекста в сообщение

```
OutboxPublisherBackgroundService.ProcessOnceAsync()
    │
    ├─ ExtractFromOutbox(TraceParent, TraceState)
    │      └─ Восстанавливает ActivityContext родительского спана из полей БД
    │
    ├─ Source.StartActivity("kafka outbox publish", Producer, parent)
    │      └─ Создаёт новый Activity (span) с родителем, делает его Activity.Current
    │
    └─ publisher.PublishRawAsync(topic, key, payload, ct)
           │
           └─ InjectCurrentContext(headers)
                  └─ Сериализует Activity.Current → заголовки traceparent + tracestate
```

**Код:**
```csharp
// OutboxPublisherBackgroundService.cs — оба сервиса идентичны
var parent = KafkaTraceContext.ExtractFromOutbox(message.TraceParent, message.TraceState);
using var activity = KafkaTraceContext.Source.StartActivity(
    "kafka outbox publish", ActivityKind.Producer, parent);

activity?.SetTag("messaging.system", "kafka");
activity?.SetTag("messaging.destination.name", message.Topic);
activity?.SetTag("messaging.message.id", message.Id.ToString());

await publisher.PublishRawAsync(message.Topic, message.MessageKey, message.Payload, ct);
```

```csharp
// KafkaEventPublisher.cs / KafkaBookingPublisher.cs — PublishRawAsync
var headers = new Headers();
KafkaTraceContext.InjectCurrentContext(headers);
await _producer.ProduceAsync(topic, new Message<string, string>
{
    Key = key,
    Value = payload,
    Headers = headers  // ← traceparent + tracestate
}, ct);
```

#### 2. Consumer: чтение контекста из сообщения

```
BookingRequestedConsumer.ExecuteAsync()
    │
    ├─ consumer.Consume(...)
    ├─ ExtractFromHeaders(consumeResult.Message.Headers)
    │      └─ Восстанавливает ActivityContext из заголовков Kafka
    │
    ├─ Source.StartActivity("kafka consume booking-requested", Consumer, parent)
    │      └─ Создаёт новый Consumer-span с родителем
    │      └─ Устанавливает OpenTelemetry-теги (messaging.system, destination.name)
    │
    └─ ProcessPrimaryPayloadAsync() / HandleMessageAsync()
```

**Код:**
```csharp
// BookingRequestedConsumer.cs — типичный consumer
var parent = KafkaTraceContext.ExtractFromHeaders(consumeResult.Message.Headers);
using var activity = KafkaTraceContext.Source.StartActivity(
    "kafka consume booking-requested", ActivityKind.Consumer, parent);

activity?.SetTag("messaging.system", "kafka");
activity?.SetTag("messaging.destination.name", TopicNames.BookingRequested);
activity?.SetTag("messaging.kafka.message_key", consumeResult.Message.Key);

var shouldCommit = await ProcessPrimaryPayloadAsync(consumeResult.Message.Value, ct);
```

### Outbox как точка сохранения трассировки

Outbox-сообщение хранит контекст в двух полях (сущность `OutboxMessage`):

| Поле БД | Тип | Назначение |
|---|---|---|
| `TraceParent` | `string?` | Полный `traceparent` активного Activity на момент записи в outbox |
| `TraceState` | `string?` | Полный `tracestate` активного Activity (опционально, часто null) |

Когда `OutboxPublisherBackgroundService` читает outbox-сообщение (спустя до 5 секунд), он восстанавливает родительский `ActivityContext` через `ExtractFromOutbox` и создаёт новый span. Таким образом, трассировка **переживает** задержку outbox polling.

### Семантические конвенции OpenTelemetry для Kafka

Span'ы следуют [OpenTelemetry Semantic Conventions for Messaging](https://opentelemetry.io/docs/specs/semconv/messaging/kafka/):

| Атрибут | Применение | Значение |
|---|---|---|
| `messaging.system` | Все спаны | `"kafka"` |
| `messaging.destination.name` | Все спаны | Имя топика (`"booking-requested"`) |
| `messaging.kafka.message_key` | Consumer | Ключ сообщения |
| `ActivityKind.Producer` | Outbox publisher | Публикация |
| `ActivityKind.Consumer` | Kafka consumer | Потребление |

### Активные топики и их спаны

| Топик | Activity имя | ActivityKind | Сервис |
|---|---|---|---|
| `booking-requested` | `kafka consume booking-requested` | Consumer | Events |
| `booking-requested-retry` | `kafka consume booking-requested-retry` | Consumer | Events |
| `booking-requested-dlq` | `kafka consume booking-requested-dlq` | Consumer | — (нет consumer'а) |
| `booking-confirmed` | `kafka consume booking-confirmed` | Consumer | Booking |
| `booking-rejected` | `kafka consume booking-rejected` | Consumer | Booking |
| `booking-not-approved` | `kafka consume booking-not-approved` | Consumer | Booking |
| `booking-cancelled` | `kafka consume booking-cancelled` | Consumer | Events |
| (Outbox → любой топик) | `kafka outbox publish` | Producer | Events / Booking |

### Сквозная трассировка: полный пример

Прохождение одного запроса `POST /bookings/{eventId}` через всю систему:

```
HTTP POST /bookings/{eventId}
    │  span: "POST /bookings/{eventId}"                traceId=A, spanId=B
    │  (создан ASP.NET Core + OTEL Instrumentation)
    │
    ├─ CreateBookingHandler → BookingService
    │   └─ SaveBooking + OutboxMessage {
    │         TraceParent: "00-A-C-01",                  spanId=C (текущий Activity)
    │         TraceState:  null
    │      }
    │
    ▼ [OutboxPublisherBackgroundService — Booking, +delay_ms]
    │
    │  ExtractFromOutbox("00-A-C-01", null)              parentId=C
    │  StartActivity("kafka outbox publish", Producer)   spanId=D, parentId=C
    │  InjectCurrentContext → traceparent="00-A-D-01"
    │
    ▼ [Kafka: booking-requested]
    │
    ├─ BookingRequestedConsumer — Events
    │   ExtractFromHeaders → parentId=D
    │   StartActivity("kafka consume booking-requested", Consumer)  spanId=E, parentId=D
    │   │
    │   ├─ Дедупликация (ProcessedMessage)
    │   ├─ Проверка существования события
    │   ├─ TryReserveSeats()
    │   └─ SaveEventAndOutboxAsync → OutboxMessage {
    │         TraceParent: "00-A-E-01",                  spanId=E
    │      }
    │
    ▼ [OutboxPublisherBackgroundService — Events, +delay_ms]
    │
    │  ExtractFromOutbox("00-A-E-01", null)              parentId=E
    │  StartActivity("kafka outbox publish", Producer)   spanId=F, parentId=E
    │  InjectCurrentContext → traceparent="00-A-F-01"
    │
    ▼ [Kafka: booking-confirmed]
    │
    └─ BookingConfirmedConsumer — Booking
        ExtractFromHeaders → parentId=F
        StartActivity("kafka consume booking-confirmed", Consumer)  spanId=G, parentId=F
        │
        └─ UpdateBookingStatus Pending → Confirmed
```

**Результат в Jaeger/Grafana Tempo:** единая trace `A` содержит 7+ спанов:
```
POST /bookings/{eventId}       (B, parent=∅)
 └─ kafka outbox publish       (D, parent=C)  ← C создан внутри BookingService
     └─ kafka consume booking-requested  (E, parent=D)
         └─ kafka outbox publish        (F, parent=E)
             └─ kafka consume booking-confirmed (G, parent=F)
```

---

## Альтернативы

| Вариант | Плюсы | Минусы | Решение |
|---|---|---|---|
| **Не трассировать Kafka** | Нет кода | Невозможно отладить сквозной сценарий; каждая ошибка требует ручного сопоставления логов | ❌ Неприемлемо |
| **Свой проприетарный CorrelationId** | Полный контроль формата | Не совместим с Jaeger/Zipkin/Tempo; нельзя визуализировать без своих инструментов | ❌ Несовместимо |
| **OpenTelemetry Baggage + свой propagation** | Стандартный API OTEL | Сложнее, дублирует W3C-заголовки | ❌ Избыточно |
| **W3C Trace Context + `KafkaTraceContext`** | Стандартный формат, совместимость со всеми APM, простой код (~50 строк), переживает Outbox | Две копии класса, ручная работа с заголовками | ✅ Выбран |

---

## Последствия

### Положительные

- Сквозная видимость: один запрос → одна trace в Grafana/Jaeger, все промежуточные шаги видны
- Визуализация задержек: waterfall-диаграмма показывает, сколько времени занял каждый этап (outbox polling, обработка в Events, доставка Kafka)
- Мгновенная локализация ошибок: span с `status=error` сразу показывает, на каком сервисе и топике произошёл сбой
- Переживает Outbox: трассировка не теряется при записи в БД, контекст сохраняется в полях `TraceParent`/`TraceState`
- Не блокирует работу: при отсутствии OTEL-экспортера `Activity.Current` = null, `InjectCurrentContext` ничего не делает, `activity?.SetTag()` — safe null-propagation
- Бесплатно: `System.Diagnostics.Activity` входит в .NET BCL, не требует лицензий
- Совместимость: W3C Trace Context понимают все APM-системы (Jaeger, Zipkin, Grafana Tempo, DataDog, New Relic, AWS X-Ray)

### Отрицательные

- Дублирование кода: `KafkaTraceContext` существует в двух идентичных копиях (Events и Booking) из-за архитектурного правила «сервисы не ссылаются друг на друга»
- Ручная инструментация: каждый consumer должен явно вызывать `ExtractFromHeaders` + `StartActivity` + `SetTag`. Пропуск — trace разрывается
- Нет автоматического propagation через Outbox: поля `TraceParent`/`TraceState` нужно явно сохранять в БД при создании OutboxMessage
- Не трассируются retry-сообщения в DLQ-топике: в `booking-requested-dlq` нет активного consumer'а, trace обрывается

### Риски

- **Разрыв трассировки при refactoring**: если новый разработчик создаст consumer без `ExtractFromHeaders` + `StartActivity`, trace начнётся заново (новый `ActivityContext`). Нужен код-ревью.
- **Рассинхронизация копий**: две копии `KafkaTraceContext` могут разойтись при будущих изменениях. Решение: вынести в общий проект `EventForge.Shared` при следующем рефакторинге.
- **Большой объем трассировочных данных**: при высоком RPS каждая trace содержит 7+ спанов. Нужно настроить sampling (например, `AlwaysOnSampler` только для ошибок, `ProbabilitySampler(0.1)` для остальных).

---

## Конфигурация OpenTelemetry

Трассировка настраивается в `Program.cs` каждого сервиса. Пример:

```csharp
// Program.cs — Events/Booking
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddSource("EventForge.Events")      // ActivitySource из KafkaTraceContext
            .AddSource("EventForge.Booking")     // ActivitySource из KafkaTraceContext
            .AddAspNetCoreInstrumentation()       // HTTP-спаны
            .AddHttpClientInstrumentation()       // HttpClient-вызовы
            .AddEntityFrameworkCoreInstrumentation() // SQL-запросы
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://jaeger:4317");
            });
    });
```

---

## Все участки кода, где работает трассировка

### Producer (запись контекста)

| Файл | Метод | Операция |
|---|---|---|
| `Events/.../KafkaEventPublisher.cs` | `PublishRawAsync` | `InjectCurrentContext(headers)` перед `ProduceAsync` |
| `Booking/.../KafkaBookingPublisher.cs` | `PublishRawAsync` | `InjectCurrentContext(headers)` перед `ProduceAsync` |
| `Events/.../OutboxPublisherBackgroundService.cs` | `ProcessOnceAsync` | `ExtractFromOutbox` → `StartActivity(Producer)` |
| `Booking/.../OutboxPublisherBackgroundService.cs` | `ProcessOnceAsync` | `ExtractFromOutbox` → `StartActivity(Producer)` |

### Consumer (чтение контекста)

| Файл | Метод | Activity имя |
|---|---|---|
| `Events/.../BookingRequestedConsumer.cs` | `ExecuteAsync` | `kafka consume booking-requested` |
| `Events/.../BookingRequestedRetryConsumer.cs` | `ExecuteAsync` | `kafka consume booking-requested-retry` |
| `Events/.../BookingCancelledConsumer.cs` | `ExecuteAsync` | `kafka consume booking-cancelled` |
| `Booking/.../BookingConfirmedConsumer.cs` | `ExecuteAsync` | `kafka consume booking-confirmed` |
| `Booking/.../BookingRejectedConsumer.cs` | `ExecuteAsync` | `kafka consume booking-rejected` |
| `Booking/.../BookingNotApprovedConsumer.cs` | `ExecuteAsync` | `kafka consume booking-not-approved` |

---

## Связанные файлы

- `EventForge.Events.Infrastructure/Services/KafkaTraceContext.cs` — реализация для Events (ActivitySource: `"EventForge.Events"`)
- `EventForge.Booking.Infrastructure/Services/KafkaTraceContext.cs` — реализация для Booking (ActivitySource: `"EventForge.Booking"`)
- `EventForge.Events.Infrastructure/Services/KafkaEventPublisher.cs` — `InjectCurrentContext` на стороне Events-producer
- `EventForge.Booking.Infrastructure/Services/KafkaBookingPublisher.cs` — `InjectCurrentContext` на стороне Booking-producer
- `EventForge.Events.Infrastructure/Services/OutboxPublisherBackgroundService.cs` — `ExtractFromOutbox` + Producer-span
- `EventForge.Booking.Infrastructure/Services/OutboxPublisherBackgroundService.cs` — `ExtractFromOutbox` + Producer-span
- `EventForge.Events.Domain/Entities/OutboxMessage.cs` — поля `TraceParent`, `TraceState`
- `EventForge.Booking.Domain/Entities/OutboxMessage.cs` — поля `TraceParent`, `TraceState`
- `EventForge.Events.Infrastructure/Services/BookingRequestedConsumer.cs` — пример consumer (W3C-заголовки → Consumer-span)
- `EventForge.Events.Infrastructure/Services/BookingRequestedRetryConsumer.cs` — retry consumer (трассировка продолжается через retry-топик)
- `deploy/otel-collector-config.yaml` — конфигурация OpenTelemetry Collector (экспорт в Jaeger)
- `deploy/grafana-dashboards/eventforge.json` — дашборд Grafana с trace-ссылками

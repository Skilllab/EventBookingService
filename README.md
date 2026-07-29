<br />
<div align="center">
  <a href="https://github.com/Skilllab/EventForge">
    <img src="logo.png" alt="Logo" width="80" height="80">
  </a>

  <h1 align="center">EventForge</h1>
  <p align="center">
    Набор микросервисов для управления пользователями, событиями и бронированием
  </p>
</div>

## Содержание

- [О проекте](#о-проекте)
- [Архитектура решения](#архитектура-решения)
- [Сервисы](#сервисы)
- [Технологии](#технологии)
- [Аутентификация и роли](#аутентификация-и-роли)
- [CQRS и внутренний Mediator](#cqrs-и-внутренний-mediator)
- [Kafka и асинхронные процессы](#kafka-и-асинхронные-процессы)
- [Запуск проекта](#запуск-проекта)
- [Наблюдаемость](#наблюдаемость)
- [Миграции](#миграции)
- [Тестирование](#тестирование)
- [API примеры](#api-примеры)

## О проекте

`EventForge` — это solution с несколькими .NET 10 микросервисами для управления пользователями, событиями и бронированиями.

Система построена вокруг асинхронного обмена сообщениями через Kafka и использует паттерн Outbox для надёжной публикации интеграционных событий.

### Сервисы

- `EventForge.Users` — регистрация, логин и JWT-аутентификация
- `EventForge.Events` — управление событиями, доступными местами и обработка Kafka-событий бронирования
- `EventForge.Booking` — создание, обработка и отмена бронирований, публикация сообщений через outbox

Решение использует слои `Domain`, `Application`, `Infrastructure`, `Presentation` для каждого сервиса.

## Архитектура решения

Схема зависимостей внутри каждого сервиса:

```text
Presentation -> Application -> Domain
                  ^
            Infrastructure
```

Общие проекты:

- `EventForge.Shared/EventForge.Contract` — контракты Kafka-сообщений
- `EventForge.Shared/EventForge.Entities` — общие перечисления и shared entities
- `EventForge.Shared/EventForge.ExceptionMiddleware` — middleware для обработки ошибок
- `EventForge.Shared/EventForge.LoggingDBInterceptor` — DB interceptor и инфраструктурные расширения
- `EventForge.Shared/EventForge.Settings` — общие настройки, включая JWT

### Основные компоненты сервисов

#### `EventForge.Users`

- `AuthService`
- `PasswordHasher`
- `JwtTokenGenerator`
- `UserRepository`

#### `EventForge.Events`

- `EventService`
- `EventRepository`
- `ProcessedMessageRepository`
- `BookingRequestedConsumer`
- `BookingConfirmedConsumer`
- `BookingRejectedConsumer`
- `BookingCancelledConsumer`
- `BookingRequestedMessageProcessor` — доменная обработка `BookingRequested` (идемпотентность, валидация, outbox)
- `BookingRequestedRetryConsumer` — обработка `booking-requested-retry` с backoff и лимитом `RetryTopicMaxAttempts`

#### `EventForge.Booking`

- `BookingService`
- `BookingRepository`
- `OutboxRepository`
- `BookingBackgroundService`
- `OutboxPublisherBackgroundService`
- `KafkaBookingConfirmedPublisher`

## Технологии

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL
- Confluent.Kafka
- xUnit 3
- FluentAssertions
- Moq
- Testcontainers for .NET

## Аутентификация и роли

Система использует JWT Bearer authentication с ролями `User` и `Admin`.

- `User` может просматривать события, создавать и отменять свои бронирования
- `Admin` дополнительно может создавать, изменять и удалять события, а также отменять любые бронирования

JWT создаётся сервисом `EventForge.Users`.

JWT содержит как минимум:

- `sub` — GUID пользователя
- `role` — `User` или `Admin`

## CQRS и внутренний Mediator

В сервисах `Booking`, `Events`, `Users` применён CQRS-подход на уровне Application-слоя:

- `Commands` — операции изменения состояния (create/update/cancel/register)
- `Queries` — операции чтения
- `Handlers` — отдельные обработчики для каждого сценария
- контроллеры делегируют выполнение через `ISender`

Для dispatch-запросов используется внутренний mediator без внешних зависимостей:

- `IRequest<TResponse>`
- `IRequestHandler<TRequest, TResponse>`
- `ISender`
- `Mediator` (резолвинг handler через DI)

Это позволяет:
- изолировать use-case’ы по файлам;
- проще тестировать бизнес-сценарии (unit-тесты на handlers);
- централизованно добавлять cross-cutting логику (валидация, аудит, логирование, pipeline-поведение).


## Kafka и асинхронные процессы

Kafka используется для межсервисного взаимодействия между `Booking` и `Events`.

### Основной поток бронирования

1. Клиент создаёт бронирование через `EventForge.Booking`
2. `Booking` сохраняет бронь в статусе `Pending`
3. `Booking` публикует `BookingRequested` в Kafka через outbox
4. `Events` читает `BookingRequested`
5. `Events`:
   - проверяет, что событие существует
   - проверяет, что событие ещё не началось
   - проверяет, что есть доступные места
   - резервирует место через доменную модель `Event`
6. Если всё успешно — публикуется `BookingConfirmed`
7. Если событие не найдено — публикуется `BookingRejected`
8. Если событие уже началось или мест нет — публикуется `BookingNotApproved`
9. `Booking` читает `BookingNotApproved` и переводит бронь в `Rejected`

### Resilience потока `BookingRequested` (retry + DLQ)

Для `BookingRequestedConsumer` используется схема отказоустойчивости:

1. Чтение из `booking-requested` с `EnableAutoCommit = false`.
2. Невалидный JSON/пустой payload -> `booking-requested-dlq`.
3. При `DbUpdateException`:
   - до `InPlaceRetryCount` быстрых локальных ретраев (exponential delay),
   - затем публикация `BookingRequestedRetryEnvelope` в `booking-requested-retry`.
4. `BookingRequestedRetryConsumer` читает `booking-requested-retry`:
   - ждет `NextAttemptAtUtc`,
   - повторно обрабатывает через `BookingRequestedMessageProcessor`.
5. Если `RetryAttempt >= RetryTopicMaxAttempts` -> сообщение переводится в `booking-requested-dlq`.
6. Commit offset выполняется только после принятого решения (success/retry/dlq), чтобы исключить потерю сообщений.


Контракты Kafka лежат в `EventForge.Shared/EventForge.Contract/Brokers`.

Используемые типы сообщений:

- `BookingRequested`
- `BookingConfirmed`
- `BookingRejected`
- `BookingNotApproved`
- `BookingCancelled`
- `BookingRequestedRetryEnvelope`
- `BookingRequestedDlqMessage`

### Идемпотентность

- `Events` хранит обработанные сообщения в `ProcessedMessages`
- `Booking` также хранит обработанные сообщения в `ProcessedMessages` для защиты от повторной доставки Kafka-сообщений
- повторная доставка Kafka-сообщения не приводит к повторной обработке
- публикация в Kafka выполняется через outbox
- `BookingRequestedMessageProcessor` централизует бизнес-обработку и повторно используется из primary/retry consumer.
- retry/dlq сообщения также отправляются через outbox, чтобы не терять события при временной недоступности Kafka.

## Запуск проекта

### Требования

- .NET 10 SDK
- Docker Desktop

### Запуск через Docker Compose

Проект запускается через `docker-compose.yml`.
Для каждого микросервиса используется отдельный `Dockerfile`:

- `EventForge.Users/dockerfile`
- `EventForge.Events/dockerfile`
- `EventForge.Booking/dockerfile`

`docker-compose` поднимает:

- `zookeeper`
- `kafka`
- `akhq`
- `kafka-init-topics`
- `postgres`
- `redis`
- `users_api`
- `events_api`
- `booking_api`
- `pgadmin`

Перед запуском нужно создать `.env` файл в корне проекта рядом с `docker-compose.yml`.


### Пример `.env`

```env
# Базовые секреты
DB_USER=postgres
DB_PASSWORD=postgres
DB_PORT=5432

# Настройки для Users
USERS_DB=eventforge_users_dev
USERS_HOST=postgres_users

# Настройки для Events
EVENTS_DB=eventforge_events_dev
EVENTS_HOST=postgres_events

# Настройки для Booking
BOOKING_DB=eventforge_booking_dev
BOOKING_HOST=postgres_booking

# Строка подключения в формате .NET Npgsql
USERS_CONNECTION_STRING=Host=${USERS_HOST};Port=${DB_PORT};Database=${USERS_DB};Username=${DB_USER};Password=${DB_PASSWORD}
EVENTS_CONNECTION_STRING=Host=${EVENTS_HOST};Port=${DB_PORT};Database=${EVENTS_DB};Username=${DB_USER};Password=${DB_PASSWORD}
BOOKING_CONNECTION_STRING=Host=${BOOKING_HOST};Port=${DB_PORT};Database=${BOOKING_DB};Username=${DB_USER};Password=${DB_PASSWORD}

EVENTS_REDIS_CONNECTION_STRING=redis:6379

# Настройки pgAdmin
PGADMIN_EMAIL=admin@admin.com
PGADMIN_PASSWORD=admin_password_987

```

### Как заполнять `.env`

#### Базы данных

- `DB_USER` — пользователь PostgreSQL для всех контейнеров БД
- `DB_PASSWORD` — пароль PostgreSQL
- `DB_PORT` — порт к PostgeSQL
- `USERS_DB` — имя БД сервиса `Users`
- `EVENTS_DB` — имя БД сервиса `Events`
- `BOOKING_DB` — имя БД сервиса `Booking`

Внутри docker-сети сами контейнеры PostgreSQL по-прежнему слушают стандартный порт `5432`.
Прир разворачивании postgres в docker-compose базы данных будут созданы скриптом.

#### Строки подключения сервисов

- `USERS_CONNECTION_STRING` — строка подключения для `users_api`
- `EVENTS_CONNECTION_STRING` — строка подключения для `events_api`
- `BOOKING_CONNECTION_STRING` — строка подключения для `booking_api`

Важно:
- в compose используется имя контейнера БД как host
- поэтому внутри connection string нужно указывать:
  - `postgres_users`
  - `postgres_events`
  - `postgres_booking`
- порт внутри сети Docker — `5432`

Пример:
`Host=postgres_users;Port=5432;Database=eventforge_users;Username=postgres;Password=postgres`

#### PgAdmin

- `PGADMIN_EMAIL` — логин для входа в PgAdmin
- `PGADMIN_PASSWORD` — пароль для входа в PgAdmin

#### Строка подключения к REDIS

`EVENTS_REDIS_CONNECTION_STRING=redis:6379` 

По сути состоит из имени сервера и порта

### Команды запуска

Сборка и запуск всех сервисов в фоновом режиме:

```bash
docker-compose up -d
```

Запуск только `Users API` с зависимостями:

```bash
docker-compose up -d users_api
```

Запуск только окружения для последующего запуска сервисов в debug режиме

```bash
docker-compose up -d zookeeper kafka akhq postgres pgadmin kafka-init-topics redis prometheus grafana jaeger
```

Сборка образов без запуска:

```bash
docker-compose build
```

Остановка и удаление контейнеров, тома при этом сохраняются:

```bash
docker-compose down
```

### Что происходит при старте

При запуске:

- каждый API-сервис собирается из своего `Dockerfile`
- сервисы запускаются в окружении `Docker`
- при старте автоматически применяются EF Core миграции
- `Booking` и `Events` подключаются к Kafka внутри docker-сети через `kafka:29092`
- Swagger доступен в окружении `Development` и `Docker`
- `Kafka-init-topics` автоматически создаёт 7 топиков (1 партиция, replication-factor 1):
  - `booking-requested` — запрос на бронирование от Booking -> Events
  - `booking-confirmed` — подтверждение бронирования от Events -> Booking
  - `booking-rejected` — отказ (событие не найдено) от Events -> Booking
  - `booking-not-approved` — отказ (нет мест / событие началось) от Events -> Booking
  - `booking-cancelled` — отмена бронирования от Booking -> Events
  - `booking-requested-retry` — отложенные повторные попытки обработки `BookingRequested`
  - `booking-requested-dlq` — сообщения, требующие ручного разбора

### Доступные порты

После запуска будут доступны:

- `Users API` — `http://localhost:5008`
- `Events API` — `http://localhost:5009`
- `Booking API` — `http://localhost:5010`
- `AKHQ` — `http://localhost:8080`
- `PgAdmin` — `http://localhost:8020`

### Настройка вне Docker

Для локального запуска без compose можно использовать `appsettings.Development.json`, user secrets или переменные окружения.

Пример для `Users`:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=eventforge_users;Username=postgres;Password=postgres" --project EventForge.Users/EventForge.Users.Presentation
```

Аналогично настраиваются `EventForge.Events.Presentation` и `EventForge.Booking.Presentation`.

### Настройка Kafka вне Docker

В `appsettings*.json` сервисов `Booking` и `Events` используется секция `KafkaOptions`.

Пример:

```json
{
  "KafkaOptions": {
    "BootstrapServers": "localhost:9092",
    "ConsumerGroup": "eventforge-events",
    "InPlaceRetryCount": 3,
    "InPlaceRetryBaseDelayMs": 200,
    "RetryTopicMaxAttempts": 5,
    "RetryTopicInitialDelaySeconds": 30,
    "RetryTopicMaxDelaySeconds": 900
  }
}
```

## Наблюдаемость

В проект добавлен базовый observability-стек:

- `Prometheus` — сбор метрик (`prometheus.yml`, `metrics_path: /metrics`)
- `Grafana` — визуализация метрик и дашборды
- `Jaeger` — трассировка (OpenTelemetry OTLP + UI)
- `AKHQ` — UI для Kafka (просмотр топиков/сообщений)

### UI и порты

| Инструмент | URL | Порт |
|---|---|---|
| Grafana | http://localhost:3300 | `3300` |
| Prometheus | http://localhost:9090 | `9090` |
| Jaeger UI | http://localhost:16686 | `16686` |
| AKHQ | http://localhost:8080 | `8080` |

### Kafka: метрики клиента и сквозная трассировка

Для Kafka добавлены:

- клиентские метрики producer/consumer (publish/consume/process counters и latency histograms);
- сквозная трассировка через Kafka headers (`traceparent`/`tracestate`);
- перенос trace-context через Outbox (`TraceParent`, `TraceState` в `OutboxMessages`);
- продолжение trace в consumer’ах с `ActivityKind.Consumer`.

Результат:
- в `Jaeger` видна непрерывная цепочка:
  `HTTP -> Command/Handler -> Outbox -> Kafka Producer -> Kafka Consumer -> DB`;
- в `Prometheus/Grafana` видны технические метрики Kafka-клиентов по сервисам.

### Как запустить стек мониторинга

Если сервисы уже запущены, поднимите только мониторинг:
```bash
docker compose up -d prometheus grafana jaeger
```

Если нужен полный локальный стенд:
```bash
docker compose up -d
```

### Дашборд Grafana `EventForge dashboard 1_0`

Файл дашборда:  
`grafana/provisioning/dashboards/files/EventForge dashboard 1_0.json`

Панели в дашборде:

- `Процент загрузки процессора (CPU)`
- `Exceptions`
- `Объем ОЗУ`
- `Активные потоки`
- `Latency (p50, p95, p99)`
- `Текущее количество запросов в обработке`
- `Throughput (RPS)`

## Миграции

Миграции создаются отдельно для каждого сервиса.

### Users

```bash
dotnet ef migrations add <MigrationName> --project EventForge.Users/EventForge.Users.Infrastructure --startup-project EventForge.Users/EventForge.Users.Presentation
```

### Events

```bash
dotnet ef migrations add <MigrationName> --project EventForge.Events/EventForge.Events.Infrastructure --startup-project EventForge.Events/EventForge.Events.Presentation
```

### Booking

```bash
dotnet ef migrations add <MigrationName> --project EventForge.Booking/EventForge.Booking.Infrastructure --startup-project EventForge.Booking/EventForge.Booking.Presentation
```

Применение миграций:

```bash
dotnet ef database update --project <InfrastructureProject> --startup-project <PresentationProject>
```

## Тестирование

В репозитории есть тестовые проекты для микросервисов в `EventForge.*/Tests`.

### Users

```bash
dotnet test EventForge.Users/Tests/EventForge.Users.UnitTests/EventForge.Users.UnitTests.csproj
dotnet test EventForge.Users/Tests/EventForge.Users.IntegrationTests/EventForge.Users.IntegrationTests.csproj
dotnet test EventForge.Users/Tests/EventForge.Users.e2eTests/EventForge.Users.e2eTests.csproj
```

### Events

```bash
dotnet test EventForge.Events/Tests/EventForge.Events.UnitTests/EventForge.Events.UnitTests.csproj
dotnet test EventForge.Events/Tests/EventForge.Events.IntegrationTests/EventForge.Events.IntegrationTests.csproj
dotnet test EventForge.Events/Tests/EventForge.Events.e2eTests/EventForge.Events.e2eTests.csproj
```

### Booking

```bash
dotnet test EventForge.Booking/Tests/EventForge.Booking.UnitTests/EventForge.Booking.UnitTests.csproj
dotnet test EventForge.Booking/Tests/EventForge.Booking.IntegrationTests/EventForge.Booking.IntegrationTests.csproj
dotnet test EventForge.Booking/Tests/EventForge.Booking.e2eTests/EventForge.Booking.e2eTests.csproj
```

Integration-тесты используют `Testcontainers.PostgreSql`, поэтому для них нужен запущенный Docker Desktop.

## Стратегия кэширования

Сервис `EventForge.Events` использует Redis для кэширования часто запрашиваемых данных. Цель — снизить нагрузку на БД при повторяющихся чтениях и ускорить ответ API.

### Что кэшируется

| Данные | Ключ | TTL (по умолчанию) |
|---|---|---|
| Одно событие | `event:{guid}` | 5 минут |
| Топ-10 событий | `events:top10` | 10 минут |

Оба ключа определены в `EventForge.CacheKeys.KeysForEvents`.

### Что НЕ кэшируется

- **Пагинированный поиск с фильтрами** (`GET /Events?title=...&page=...`) — слишком много уникальных комбинаций фильтров, попадание в кэш маловероятно, а инвалидация при любом изменении любого события была бы неоправданно сложной.
- **Операции записи** (`POST`, `PUT`, `DELETE`) — только читают/пишут БД и инвалидируют затронутые кэш-ключи.

### Стратегия инвалидации

Используется **инвалидация при изменении** (cache-aside + write-invalidate):

| Операция | Инвалидируемые ключи |
|---|---|
| `ChangeEventAsync` | `event:{id}` |
| `CancelEventAsync` | `event:{id}` |
| `ReleaseSeatAsync` | `event:{id}`, `events:top10` |
| `BookingRequestedConsumer` (бронь подтверждена) | `event:{id}`, `events:top10` |

**Примечание:** ключ `events:top10` инвалидируется только при изменении заполненности мест (`ReleaseSeatAsync`, `BookingRequestedConsumer`). При удалении события (`CancelEventAsync`) этот агрегированный кэш принудительно не сбрасывается и обновляется по TTL. Это оправдано, так как удаления происходят редко и полная очистка топа для них неэффективна.

### Защита от лавины запросов (cache stampede)

Кэширующие методы `GetEventAsync` и `GetTop10EventsAsync` используют **double-check locking** через `ConcurrentDictionary<string, SemaphoreSlim>`:

1. Быстрая проверка кэша (без блокировки).
2. Если промах — захват `SemaphoreSlim` для данного ключа.
3. Повторная проверка кэша (другой поток мог уже заполнить).
4. Только **один** поток идёт в БД, остальные ждут и получают результат из кэша.

Это гарантирует, что при истечении TTL на горячий ключ в БД уйдёт ровно один запрос, а не N параллельных.

### Отказоустойчивость

Интерфейс `ICacheService` абстрагирует работу с Redis. Если Redis недоступен:
- `GetStringAsync` возвращает `null` → сервис прозрачно идёт в БД.
- `SetStringAsync` и `RemoveAsync` silently fail (ошибки логируются, но не пробрасываются наружу).

Таким образом, Redis является опциональным ускорителем, а не критической зависимостью.

### Конфигурация

Настройки Redis задаются в `appsettings.json` секцией `RedisOptions`:

```json
{
  "RedisOptions": {
    "SingleEventExpirationMinutes": 5,
    "TopEventsExpirationMinutes": 10
  }
}
```

Строка подключения к Redis задаётся в секции `ConnectionStrings`:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

## API примеры

### Users API

Регистрация пользователя:

```bash
curl -X POST 'http://localhost:5008/auth/register' \
  -H 'Content-Type: application/json' \
  -d '{
    "login": "admin-user",
    "password": "SecurePassword123!",
    "role": "Admin"
  }'
```

Логин:

```bash
curl -X POST 'http://localhost:5008/auth/login' \
  -H 'Content-Type: application/json' \
  -d '{
    "login": "admin-user",
    "password": "SecurePassword123!"
  }'
```

### Events API

Создание события под `Admin`:

```bash
curl -X POST 'http://localhost:5009/Events' \
  -H 'Authorization: Bearer <JWT>' \
  -H 'Content-Type: application/json' \
  -d '{
    "title": "DotNet Meetup",
    "description": "Community event",
    "startAt": "2026-08-15T10:00:00Z",
    "endAt": "2026-08-15T13:00:00Z",
    "totalSeats": 50
  }'
```

Получение списка событий:

```bash
curl 'http://localhost:5009/Events?title=dotnet&page=1&pageSize=10'
```

### Booking API

Создание бронирования:

```bash
curl -X POST 'http://localhost:5010/Bookings/<EVENT_ID>' \
  -H 'Authorization: Bearer <JWT>'
```

Получение бронирования:

```bash
curl 'http://localhost:5010/Bookings/<BOOKING_ID>' \
  -H 'Authorization: Bearer <JWT>'
```

Отмена бронирования:

```bash
curl -X DELETE 'http://localhost:5010/Bookings/<BOOKING_ID>' \
  -H 'Authorization: Bearer <JWT>'
```

## Примечания

- Для полного end-to-end сценария между сервисами, кроме PostgreSQL, потребуется Kafka broker

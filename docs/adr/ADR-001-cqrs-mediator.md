# ADR-001: Внедрение CQRS и внутреннего Mediator

| Поле | Значение |
|---|---|
| **Статус** | Accepted |
| **Дата** | 01.08.2026 |
| **Автор** | skilllab |
| **Затрагивает** | Users, Events, Booking (все три Application-слоя) |
| **Связанные ADR** | — (первый) |

---

## Контекст

Контроллеры всех трёх микросервисов напрямую вызывали сервисы (`AuthService`, `EventService`, `BookingService`). Для каждого нового сценария приходилось либо добавлять метод в раздувающийся сервис, либо плодить новые сервисы. Отсутствовала единая точка для cross-cutting-логики (валидация, логирование, метрики), и каждый контроллер дублировал проверки входных параметров.

Требовалось:
- изолировать бизнес-сценарии в отдельные классы;
- добавить сквозную валидацию команд/запросов;
- собирать метрики и логи по каждому use-case;
- не вводить внешних зависимостей (MediatR и т.п.).

---

## Решение

Реализован **собственный CQRS-mediator** в общем проекте `EventForge.Shared/EventForge.CQRS`.

### Ключевые абстракции

```
IRequest<TResponse>                     // маркер команды/запроса
IRequestHandler<TRequest, TResponse>    // обработчик
ISender                                 // точка входа (Send<T>(request, ct))
Mediator : ISender                      // резолвинг handler + pipeline
```

### Pipeline behaviors

```
IPipelineBehavior<TRequest, TResponse>  // звено конвейера
    ├── ValidationBehavior              // IRequestValidator<T>.Validate()
    ├── LoggingBehavior                 // CQRS start/success/failed
    └── MetricsBehavior                 // cqrs_requests_total, cqrs_request_duration_ms
```

`Mediator.SendInternal` строит цепочку `behaviors → handler` и вызывает её. Behaviors вызываются в порядке регистрации в DI (Validation → Logging → Metrics → Handler).

### Валидаторы

Каждый Command/Query может иметь опциональный валидатор через `IRequestValidator<TRequest>`. Если валидатор не зарегистрирован, `ValidationBehavior` получает пустой `IEnumerable` и молча пропускает проверку.

**Существующие валидаторы**:

| Сервис | Команда/Запрос | Проверки |
|---|---|---|
| Users | `RegisterUserCommand` | Login не пустой, длина 3–64 симв., Password не пустой, длина ≥ 6 симв., Role (если задана) — валидный `RoleType` |
| Users | `LoginUserQuery` | Login + Password не пустые |
| Events | `CreateEventCommand` | Event ≠ null, Title не пустой, StartAt > now, StartAt < EndAt |
| Events | `ChangeEventCommand` | Event ≠ null, EventId ≠ Guid.Empty, Title (если задан) не пустой, StartAt/EndAt (если заданы) > now, StartAt < EndAt |
| Booking | `CreateBookingCommand` | EventId ≠ Guid.Empty, UserId ≠ Guid.Empty |
| Booking | `CancelBookingCommand` | BookingId ≠ Guid.Empty, UserId ≠ Guid.Empty |

Проверки вызываются автоматически через `ValidationBehavior` — контроллеру не нужно вызывать валидацию явно. При нарушении валидатор бросает `ValidationCustomException` (наследник `DomainException`), которая перехватывается `GlobalExceptionHandlingMiddleware` и возвращает клиенту 400.

### Регистрация в DI

В каждом сервисе (`Booking`, `Events`, `Users`) в `Application/DependencyInjection.cs`:

```csharp
// Mediator
services.AddScoped<ISender, Mediator>();

// Handlers (каждый use-case — отдельный handler)
services.AddScoped<IRequestHandler<CreateEventCommand, EventDTO>, CreateEventHandler>();

// Pipeline behaviors (порядок регистрации = порядок вызова)
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));

// Валидаторы
services.AddScoped<IRequestValidator<CreateEventCommand>, CreateEventCommandValidator>();
```

### Использование в контроллере

```csharp
// Было (прямой вызов сервиса):
var result = await _eventService.CreateEventAsync(dto, ct);

// Стало (через CQRS):
var command = new CreateEventCommand(dto);
var result = await _sender.Send(command, ct);
```

Контроллер внедряет только `ISender` и делегирует ему всю работу.

---

## Альтернативы

| Вариант | Плюсы | Минусы | Решение |
|---|---|---|---|
| **MediatR** | Готовый pipeline, behaviours, notification | Внешняя зависимость, магия рефлексии, сложный дебаг, неочевидный порядок behaviours | ❌ Отклонён |
| **Прямой вызов сервисов (статус-кво)** | Простота, нет новых абстракций | Раздутые сервисы, дублирование валидации, нет pipeline | ❌ Отклонён |
| **Свой Mediator** | Полный контроль, нет внешних зависимостей, понятный дебаг, behaviours в явном порядке | ~55 строк кода, рефлексия при `Send` | ✅ Выбран |

---

## Последствия

### Положительные

- Каждый use-case изолирован в отдельный Handler (один файл — один сценарий, легко найти и изменить)
- Валидация выполняется автоматически через pipeline, контроллеры стали тоньше и не содержат проверок
- Метрики (`cqrs_requests_total`, `cqrs_request_duration_ms`) доступны в Prometheus/Grafana по каждому Command/Query
- Логи содержат имя команды и результат (start/success/failed) без дополнительного кода в handler'ах
- Легко добавить новое поведение (аудит, ретраи, rate limiting) — достаточно реализовать `IPipelineBehavior<,>`
- Модульные тесты валидаторов не требуют инфраструктуры (чистые unit-тесты)

### Отрицательные

- Увеличилось количество классов: Handlers, Validators, Commands/Queries (для простых операций это overhead)
- Рефлексия в `Mediator.Send` (один вызов `MakeGenericMethod` на запрос) добавляет микро-задержку
- Разработчик должен знать, что валидация происходит неявно через pipeline, а не в контроллере

### Риски

- **Производительность рефлексии**: незначительна для типичного RPS (единицы микросекунд), но при high-load (> 10K RPS) стоит замерить и при необходимости закэшировать делегаты
- **Пропуск валидации**: если валидатор не зарегистрирован в DI, `ValidationBehavior` получит пустой `IEnumerable` и молча пропустит проверку. Требуется дисциплина при добавлении новых Commands/Queries.
- **Порядок behaviours**: изменение порядка регистрации в DI меняет порядок вызова. Сейчас Validation → Logging → Metrics, чтобы метрики считали время логирования, а логи не писались при ошибке валидации.

---

## Метрики

После внедрения доступны:

| Метрика | Тип | Теги |
|---|---|---|
| `cqrs_requests_total` | Counter | `request` (имя команды/запроса), `result` (success/error) |
| `cqrs_request_duration_ms` | Histogram | `request` (имя команды/запроса) |

Эти метрики экспортируются в Prometheus через OpenTelemetry и визуализируются в Grafana (дашборд `EventForge dashboard 1_0`).

---

## Тестирование

Для каждого валидатора написаны модульные тесты в соответствующих UnitTests-проектах:

- `EventForge.Events.UnitTests/ValidatorsTests.cs` — `CreateEventCommandValidator`, `ChangeEventCommandValidator`
- `EventForge.Booking.UnitTests/ValidatorsTests.cs` — `CreateBookingCommandValidator`, `CancelBookingCommandValidator`
- `EventForge.Users.UnitTests/ValidatorsTests.cs` — `RegisterUserCommandValidator`, `LoginUserQueryValidator`

Тесты покрывают:
- Валидные данные (исключение не выбрасывается)
- Граничные значения (ровно 3/64 символа, StartAt == now, пароль ровно 6 символов)
- Невалидные данные (Guid.Empty, пустые строки, даты в прошлом, некорректная роль)

---

## Связанные файлы

- `EventForge.Shared/EventForge.CQRS/` — ядро CQRS:
  - `ISender.cs` — точка входа
  - `Mediator.cs` — реализация (~55 строк)
  - `IRequest.cs`, `IRequestHandler.cs` — контракты
  - `IRequestValidator.cs` — интерфейс валидатора
  - `Behaviors/IPipelineBehavior.cs` — интерфейс звена конвейера
  - `Behaviors/ValidationBehavior.cs` — валидация
  - `Behaviors/LoggingBehavior.cs` — логирование
  - `Behaviors/MetricsBehavior.cs` — метрики
- `EventForge.Users.Application/DependencyInjection.cs` — регистрация Handlers + Behaviors
- `EventForge.Events.Application/DependencyInjection.cs` — регистрация Handlers + Behaviors
- `EventForge.Booking.Application/DependencyInjection.cs` — регистрация Handlers + Behaviors
- `EventForge.Users.Presentation/Controllers/AuthController.cs` — пример использования `ISender`
- `EventForge.Events.Presentation/Controllers/EventsController.cs` — пример использования `ISender`
- `EventForge.Booking.Presentation/Controllers/BookingsController.cs` — пример использования `ISender`

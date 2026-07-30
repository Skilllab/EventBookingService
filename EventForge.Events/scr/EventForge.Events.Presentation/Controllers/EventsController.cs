using System.ComponentModel.DataAnnotations;

using EventForge.CQRS;
using EventForge.Events.Application.CQRS.Commands;
using EventForge.Events.Application.CQRS.Queries;
using EventForge.Events.Presentation.DTO;
using EventForge.Events.Presentation.Mapping;
using EventForge.Shared.Constants;
using EventForge.Shared.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

namespace EventForge.Events.Presentation.Controllers;

/// <summary>
/// Контроллер для управления событиями (создание, получение, обновление, удаление)
/// </summary>
/// <param name="sender">Сервис для отправки запросов и команд</param>
/// <param name="logger">Логгер</param>
[Authorize(Policy = StringConstants.CustomJwtPolicy)]
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class EventsController(ISender sender, ILogger<EventsController> logger) : ControllerBase
{
    /// <summary>
    /// Получить список всех событий с возможностью фильтрации
    /// </summary>
    /// <param name="filterRequest">Фильтры для поиска событий (категория, статус, временной диапазон)</param>
    /// <param name="ct">Токен отмены</param>
    /// <remarks>
    /// Поддерживается постраничная выдача
    /// </remarks>
    [AllowAnonymous]
    [HttpGet]
    [Tags("API для событий")]
    [SwaggerOperation(Summary = "Получить список всех событий с возможностью фильтрации")]
    [SwaggerResponse(StatusCodes.Status200OK, "Список событий успешно получен")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректные параметры фильтрации")]
    public async Task<IActionResult> GetAllEvents([FromQuery] EventsFilterRequest filterRequest, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса GET {methodName}", nameof(GetAllEvents));

        var result = await sender.Send(new GetEventsQuery(filterRequest.ToAppDto()), ct);
        return Ok(result.ToWebDto());
    }


    /// <summary>
    /// Получить список ТОП 10 событий
    /// </summary>
    /// <param name="ct">Токен отмены</param>
    /// <remarks>
    /// Рейтинг популярности рассчитывается по количеству подтверждённых бронирований.
    /// </remarks>
    [AllowAnonymous]
    [HttpGet("top")]
    [Tags("API для событий")]
    [SwaggerOperation(Summary = "Получить список ТОП 10 событий")]
    [SwaggerResponse(StatusCodes.Status200OK, "Список ТОП 10 событий успешно получен")]
    public async Task<IActionResult> GetTop10Events(CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса GET {methodName}", nameof(GetTop10Events));

        var top = await sender.Send(new GetTop10EventsQuery(), ct);
        return Ok(top.ToWebDto());
    }

    /// <summary>
    /// Получить событие по идентификатору
    /// </summary>
    /// <param name="eventId">Идентификатор события</param>
    /// <param name="ct">Токен отмены</param>
    /// <remarks>
    /// Возвращает полную информацию о событии: название, описание, категорию, дату, количество доступных мест.
    /// </remarks>
    [AllowAnonymous]
    [HttpGet("{eventId:guid}")]
    [Tags("API для событий")]
    [SwaggerOperation(Summary = "Получить событие по идентификатору")]
    [SwaggerResponse(StatusCodes.Status200OK, "Событие успешно получено")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректный формат идентификатора события")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Событие с указанным идентификатором не найдено")]
    public async Task<IActionResult> GetEvent([Required] Guid eventId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса GET {methodName} по id: {id} ", nameof(GetEvent), eventId);

        var one = await sender.Send(new GetEventByIdQuery(eventId), ct);
        return Ok(one.ToWebDto());
    }

    /// <summary>
    /// Создать новое событие
    /// </summary>
    /// <param name="request">Данные для создания события (название, описание, категория, дата, количество мест)</param>
    /// <param name="ct">Токен отмены</param>
    /// <remarks>
    /// Доступно только роли Admin
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = nameof(RoleType.Admin))]
    [Tags("API для событий")]
    [SwaggerOperation(Summary = "Создать новое событие")]
    [SwaggerResponse(StatusCodes.Status201Created, "Событие успешно создано. Возвращает созданное событие в теле ответа")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректные данные события (невалидная модель)")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не аутентифицирован")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Доступ запрещён, требуется роль Admin")]
    public async Task<IActionResult> CreateEvent([FromBody][Required] CreateEventRequest request, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса POST {methodName}", nameof(CreateEvent));

        var created = await sender.Send(new CreateEventCommand(request.ToAppDto()), ct);
        return CreatedAtAction(nameof(CreateEvent), new { id = created.Id }, created.ToWebDto());
    }

    /// <summary>
    /// Обновить существующее событие целиком
    /// </summary>
    /// <param name="eventId">Идентификатор события (GUID)</param>
    /// <param name="request">Новые данные события (название, описание, категория, дата, количество мест)</param>
    /// <param name="ct">Токен отмены</param>
    /// <remarks>
    /// Доступно только роли Admin. Обновляются все переданные поля.
    /// </remarks>
    [Authorize(Roles = nameof(RoleType.Admin))]
    [HttpPut("{eventId:guid}")]
    [Tags("API для событий")]
    [SwaggerOperation(Summary = "Обновить существующее событие целиком")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Событие успешно обновлено")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректные данные события или идентификатор")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не аутентифицирован")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Доступ запрещён, требуется роль Admin")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Событие с указанным идентификатором не найдено")]
    public async Task<IActionResult> ChangeEvent([Required] Guid eventId, [FromBody] UpdateEventRequest request, CancellationToken ct)
    {

        logger.LogDebug("Обработка запроса PUT {methodName} c id: {id}", nameof(ChangeEvent), eventId);

        await sender.Send(new ChangeEventCommand(eventId, request.ToAppDto()), ct);
        return NoContent();
    }

    /// <summary>
    /// Отменить событие (мягкое удаление)
    /// </summary>
    /// <param name="eventId">Идентификатор события</param>
    /// <param name="ct">Токен отмены</param>
    /// <remarks>
    /// Доступно только роли Admin. Событие переводится в статус <c>Cancelled</c>.
    /// Все связанные бронирования автоматически отклоняются через Kafka.
    /// </remarks>
    [Authorize(Roles = nameof(RoleType.Admin))]
    [HttpDelete("{eventId:guid}")]
    [Tags("API для событий")]
    [SwaggerOperation(Summary = "Отменить событие (мягкое удаление)")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Событие успешно отменено")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректный идентификатор события")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не аутентифицирован")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Доступ запрещён — требуется роль Admin")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Событие с указанным идентификатором не найдено")]
    public async Task<IActionResult> CancelEvent([Required] Guid eventId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса DELETE {methodName} с id: {id}", nameof(CancelEvent), eventId);

        await sender.Send(new CancelEventCommand(eventId), ct);
        return NoContent();
    }
}

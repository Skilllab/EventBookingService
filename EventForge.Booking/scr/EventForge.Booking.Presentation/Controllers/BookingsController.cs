using System.ComponentModel.DataAnnotations;

using EventForge.Booking.Application.CQRS.Commands;
using EventForge.Booking.Application.CQRS.Queries;
using EventForge.CQRS;
using EventForge.Shared.Constants;
using EventForge.Shared.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

using Swashbuckle.AspNetCore.Annotations;

namespace EventForge.Booking.Presentation.Controllers;

/// <summary>
/// Контроллер для управления бронированиями (создание, получение, отмена)
/// </summary>
/// <param name="sender">Сервис отправки команд и запросов</param>
/// <param name="logger">Логгер</param>
[Authorize(Policy = StringConstants.CustomJwtPolicy)]
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class BookingsController(ISender sender, ILogger<BookingsController> logger) : ControllerBase
{
    /// <summary>
    /// Создать новое бронирование для указанного события
    /// </summary>
    /// <param name="eventId">Идентификатор события</param>
    /// <param name="ct">Токен отмены</param>
    /// <remarks>
    /// Бронирование создаётся асинхронно: запрос отправляется через Outbox в Kafka в сервис Events.
    /// Статус брони сразу <c>Pending</c>, финальный статус (<c>Confirmed</c> / <c>Rejected</c>)
    /// будет известен после обработки в Events-сервисе.
    /// Пользователь определяется автоматически по JWT-токену.
    /// </remarks>
    [HttpPost("{eventId:guid}")]
    [Tags("API для бронирования")]
    [SwaggerOperation(Summary = "Создать новое бронирование для указанного события")]
    [SwaggerResponse(StatusCodes.Status202Accepted, "Бронирование принято в обработку. Возвращает информацию о созданном бронировании")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректный идентификатор события")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не аутентифицирован или не удалось извлечь ID из токена")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Событие с указанным идентификатором не найдено")]
    public async Task<IActionResult> CreateBooking([Required] Guid eventId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса POST {methodName}. Создание бронирования: {eventId}", nameof(CreateBooking), eventId);

        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userIdClaim?.Value) || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Не удалось определить идентификатор пользователя.");

        var bookingInfo = await sender.Send(new CreateBookingCommand(eventId, userId), ct);
        return Accepted(bookingInfo);
    }

    /// <summary>
    /// Получить информацию о конкретном бронировании по ID
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования</param>
    /// <param name="ct">Токен отмены</param>
    /// <remarks>
    /// Возвращает полную информацию: статус бронирования, связанное событие, время создания.
    /// </remarks>
    [HttpGet("{bookingId:guid}")]
    [Tags("API для бронирования")]
    [SwaggerOperation(Summary = "Получить информацию о конкретном бронировании по ID")]
    [SwaggerResponse(StatusCodes.Status200OK, "Информация о бронировании успешно получена")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректный идентификатор бронирования")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не аутентифицирован")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Бронирование с указанным идентификатором не найдено")]
    public async Task<IActionResult> GetBooking([Required] Guid bookingId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса GET {methodName}. Получение информации для бронирования: {bookingId}", nameof(GetBooking), bookingId);

        var bookingInfo = await sender.Send(new GetBookingByIdQuery(bookingId), ct);
        return Ok(bookingInfo);
    }

    /// <summary>
    /// Получить список всех бронирований
    /// </summary>
    /// <param name="ct">Токен отмены</param>
    /// <remarks>
    /// Возвращает все бронирования в системе.
    /// Доступно только роли Admin.
    /// </remarks>
    [HttpGet]
    [Tags("API для бронирования")]
    [SwaggerOperation(Summary = "Получить список всех бронирований")]
    [SwaggerResponse(StatusCodes.Status200OK, "Список бронирований успешно получен")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не аутентифицирован или не удалось определить роль")]
    public async Task<IActionResult> GetAllBooking(CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса GET {methodName}. Получение всех бронирований", nameof(GetBooking));

        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);
        var roleClaim = User.FindFirst("role");

        if (string.IsNullOrEmpty(userIdClaim?.Value) || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Не удалось определить идентификатор пользователя.");

        if (string.IsNullOrWhiteSpace(roleClaim?.Value) || !Enum.TryParse<RoleType>(roleClaim.Value, true, out var userRole))
            return Unauthorized("Не удалось определить роль пользователя.");

        var bookingInfo = await sender.Send(new GetAllBookingsQuery(userId, userRole), ct);
        return Ok(bookingInfo);
    }

    /// <summary>
    /// Отменить бронирование
    /// </summary>
    /// <param name="bookingId">Идентификатор бронирования</param>
    /// <param name="ct">Токен отмены</param>
    /// <remarks>
    /// Обычный пользователь может отменить только своё бронирование.
    /// Администратор может отменить любое бронирование.
    /// После отмены событие освобождения мест отправляется в Events-сервис через Outbox в Kafka.
    /// </remarks>
    [HttpDelete("{bookingId:guid}")]
    [Tags("API для бронирования")]
    [SwaggerOperation(Summary = "Отменить бронирование")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Бронирование успешно отменено")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Некорректный идентификатор бронирования")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Пользователь не аутентифицирован или не удалось определить роль")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Доступ запрещён — нельзя отменить чужое бронирование (не Admin)")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Бронирование с указанным идентификатором не найдено")]
    public async Task<IActionResult> CancelBooking([Required] Guid bookingId, CancellationToken ct)
    {
        logger.LogDebug("Обработка запроса DELETE {methodName}. Удаление бронирования: {bookingId}", nameof(CancelBooking), bookingId);

        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);
        var roleClaim = User.FindFirst("role");

        if (string.IsNullOrEmpty(userIdClaim?.Value) || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Не удалось определить идентификатор пользователя.");

        if (string.IsNullOrWhiteSpace(roleClaim?.Value) || !Enum.TryParse<RoleType>(roleClaim.Value, true, out var userRole))
            return Unauthorized("Не удалось определить роль пользователя.");

        await sender.Send(new CancelBookingCommand(bookingId, userId, userRole), ct);
        return NoContent();
    }
}

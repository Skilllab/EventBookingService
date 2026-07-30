using EventForge.CQRS;
using EventForge.Events.Application.CQRS.Queries;
using EventForge.Events.Application.DTO;
using EventForge.Events.Application.Interfaces;

namespace EventForge.Events.Application.CQRS.Handlers;

/// <summary>
/// Обработчик запроса получения события по идентификатору
/// </summary>
/// <param name="eventService">Сервис для работы с событиями</param>
public sealed class GetEventByIdQueryHandler(IEventService eventService)
    : IRequestHandler<GetEventByIdQuery, EventDTO>
{
    /// <summary>
    /// Обработчик запроса получения события по идентификатору
    /// </summary>
    /// <param name="request">Запрос получения события по идентификатору</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат выполнения запроса</returns>
    public Task<EventDTO> Handle(GetEventByIdQuery request, CancellationToken ct) =>
        eventService.GetEventAsync(request.EventId, ct);
}

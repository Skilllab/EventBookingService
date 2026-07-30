using EventForge.CQRS;
using EventForge.Events.Application.CQRS.Queries;
using EventForge.Events.Application.DTO;
using EventForge.Events.Application.Interfaces;

namespace EventForge.Events.Application.CQRS.Handlers;

/// <summary>
/// Обработчик запроса получения топ 10 событий
/// </summary>
/// <param name="eventService">Сервис для работы с событиями</param>
public sealed class GetTop10EventsQueryHandler(IEventService eventService)
    : IRequestHandler<GetTop10EventsQuery, PaginatedResultTop10DTO>
{
    /// <summary>
    /// Обработчик запроса получения топ 10 событий
    /// </summary>
    /// <param name="request">Запрос получения топ 10 событий</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат выполнения запроса</returns>
    public Task<PaginatedResultTop10DTO> Handle(GetTop10EventsQuery request, CancellationToken ct) =>
        eventService.GetTop10EventsAsync(ct);
}

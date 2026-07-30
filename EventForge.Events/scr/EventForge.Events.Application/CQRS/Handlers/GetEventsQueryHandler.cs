using EventForge.CQRS;
using EventForge.Events.Application.CQRS.Queries;
using EventForge.Events.Application.DTO;
using EventForge.Events.Application.Interfaces;

namespace EventForge.Events.Application.CQRS.Handlers;

/// <summary>
/// Обработчик запроса на получение топ-10 событий с наибольшим процентом проданных мест
/// </summary>
/// <param name="eventService">Сервис для работы с событиями</param>
public sealed class GetEventsQueryHandler(IEventService eventService)
    : IRequestHandler<GetEventsQuery, PaginatedResultDTO>
{
    /// <summary>
    /// Обрабатывает запрос на получение топ-10 событий с наибольшим процентом проданных мест
    /// </summary>
    /// <param name="request">Запрос на получение событий</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат с пагинацией</returns>
    public Task<PaginatedResultDTO> Handle(GetEventsQuery request, CancellationToken ct) =>
        eventService.GetEventsAsync(request.Filter, ct);
}

using EventForge.CQRS;
using EventForge.Events.Application.DTO;

namespace EventForge.Events.Application.CQRS.Queries;

/// <summary>
/// Запрос на получение топ-10 событий с наибольшим процентом проданных мест
/// </summary>
/// <param name="Filter">Фильтр для поиска событий</param>
public sealed record GetEventsQuery(EventsFilterDTO Filter) : IRequest<PaginatedResultDTO>;

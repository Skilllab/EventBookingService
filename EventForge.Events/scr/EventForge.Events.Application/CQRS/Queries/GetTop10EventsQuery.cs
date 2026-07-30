using EventForge.CQRS;
using EventForge.Events.Application.DTO;

namespace EventForge.Events.Application.CQRS.Queries;

/// <summary>
/// Запрос получения топ 10 событий
/// </summary>
public sealed record GetTop10EventsQuery() : IRequest<PaginatedResultTop10DTO>;

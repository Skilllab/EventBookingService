using EventForge.CQRS;
using EventForge.Events.Application.DTO;

namespace EventForge.Events.Application.CQRS.Queries;

/// <summary>
/// Запрос получения события по идентификатору
/// </summary>
/// <param name="EventId">Идентификатор события</param>
public sealed record GetEventByIdQuery(Guid EventId) : IRequest<EventDTO>;

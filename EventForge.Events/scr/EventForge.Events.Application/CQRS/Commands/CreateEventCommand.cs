using EventForge.CQRS;
using EventForge.Events.Application.DTO;

namespace EventForge.Events.Application.CQRS.Commands;

/// <summary>
/// Команда для создания события
/// </summary>
/// <param name="Event">Данные для создания события</param>
public sealed record CreateEventCommand(CreateEventDto Event) : IRequest<EventDTO>;

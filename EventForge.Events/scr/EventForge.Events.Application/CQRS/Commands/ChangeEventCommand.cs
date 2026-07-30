using EventForge.CQRS;
using EventForge.Events.Application.DTO;

namespace EventForge.Events.Application.CQRS.Commands;

/// <summary>
/// Команда для изменения события
/// </summary>
/// <param name="EventId">Идентификатор события</param>
/// <param name="Event">Данные для обновления события</param>
public sealed record ChangeEventCommand(Guid EventId, UpdateEventDto Event) : IRequest<bool>;

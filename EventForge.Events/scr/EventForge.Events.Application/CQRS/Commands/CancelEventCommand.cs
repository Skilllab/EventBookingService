using EventForge.CQRS;

namespace EventForge.Events.Application.CQRS.Commands;

/// <summary>
/// Команда для отмены события
/// </summary>
/// <param name="EventId">Идентификатор события</param>
public sealed record CancelEventCommand(Guid EventId) : IRequest<bool>;

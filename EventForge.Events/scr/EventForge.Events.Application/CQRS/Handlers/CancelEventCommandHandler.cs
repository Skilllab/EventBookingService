using EventForge.CQRS;
using EventForge.Events.Application.CQRS.Commands;
using EventForge.Events.Application.Interfaces;

namespace EventForge.Events.Application.CQRS.Handlers;

/// <summary>
/// Обработчик команды отмены события
/// </summary>
/// <param name="eventService">Сервис для работы с событиями</param>
public sealed class CancelEventCommandHandler(IEventService eventService)
    : IRequestHandler<CancelEventCommand, bool>
{
    /// <summary>
    /// Обработчик команды отмены события
    /// </summary>
    /// <param name="request">Команда отмены события</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат выполнения команды</returns>
    public async Task<bool> Handle(CancelEventCommand request, CancellationToken ct)
    {
        await eventService.CancelEventAsync(request.EventId, ct);
        return true;
    }
}

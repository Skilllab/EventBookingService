using EventForge.CQRS;
using EventForge.Events.Application.CQRS.Commands;
using EventForge.Events.Application.Interfaces;

namespace EventForge.Events.Application.CQRS.Handlers;

/// <summary>
/// Обработчик команды изменения события
/// </summary>
/// <param name="eventService">Сервис для работы с событиями</param>
public sealed class ChangeEventCommandHandler(IEventService eventService)
    : IRequestHandler<ChangeEventCommand, bool>
{
    /// <summary>
    /// Обработчик команды изменения события
    /// </summary>
    /// <param name="request">Команда изменения события</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат выполнения команды</returns>
    public async Task<bool> Handle(ChangeEventCommand request, CancellationToken ct)
    {
        await eventService.ChangeEventAsync(request.EventId, request.Event, ct);
        return true;
    }
}

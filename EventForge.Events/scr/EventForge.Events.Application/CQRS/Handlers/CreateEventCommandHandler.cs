using EventForge.CQRS;
using EventForge.Events.Application.CQRS.Commands;
using EventForge.Events.Application.DTO;
using EventForge.Events.Application.Interfaces;

namespace EventForge.Events.Application.CQRS.Handlers;

/// <summary>
/// Обработчик команды создания события
/// </summary>
/// <param name="eventService">Сервис для работы с событиями</param>
public sealed class CreateEventCommandHandler(IEventService eventService)
    : IRequestHandler<CreateEventCommand, EventDTO>
{
    /// <summary>
    /// Обработчик команды создания события
    /// </summary>
    /// <param name="request">Команда создания события</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат выполнения команды</returns>
    public Task<EventDTO> Handle(CreateEventCommand request, CancellationToken ct) =>
        eventService.CreateEventAsync(request.Event, ct);
}

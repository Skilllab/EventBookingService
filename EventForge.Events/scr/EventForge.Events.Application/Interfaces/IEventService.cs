using EventForge.Events.Application.DTO;

namespace EventForge.Events.Application.Interfaces;

/// <summary>
/// Основной интерфейс сервиса
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Создание события
    /// </summary>
    /// <param name="currentEvent">Входящая DTO</param>
    /// <param name="ct">Токен отмены</param>
    Task<EventDTO> CreateEventAsync(CreateEventDto currentEvent, CancellationToken ct);

    /// <summary>
    /// Отмена события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task CancelEventAsync(Guid eventId, CancellationToken ct);

    /// <summary>
    /// Получение событий с пагинацией
    /// </summary>
    /// <param name="filter">Фильтр для применения пагинации</param>
    /// <param name="ct">Токен отмены</param>
    Task<PaginatedResultDTO> GetEventsAsync(EventsFilterDTO filter, CancellationToken ct);

    /// <summary>
    /// Поиск события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task<EventDTO> GetEventAsync(Guid eventId, CancellationToken ct);

    /// <summary>
    /// Изменение события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="currentEvent">Свойства из DTO для обновления события</param>
    /// <param name="ct">Токен отмены</param>
    Task ChangeEventAsync(Guid eventId, UpdateEventDto currentEvent, CancellationToken ct);

    /// <summary>
    /// Освободить одно место на событии.
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="ct">Токен отмены</param>
    Task ReleaseSeatAsync(Guid eventId, CancellationToken ct);

    /// <summary>
    /// Получить ТОП 10 событий
    /// </summary>
    /// <param name="ct"></param>
    Task<PaginatedResultTop10DTO> GetTop10EventsAsync(CancellationToken ct);
}

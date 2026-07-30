using System.Collections.Concurrent;
using System.Text.Json;

using EventForge.CacheKeys;
using EventForge.Events.Application.DTO;
using EventForge.Events.Application.Entities;
using EventForge.Events.Application.Interfaces;
using EventForge.Events.Application.Mapping;
using EventForge.Events.Domain.Entities;
using EventForge.Events.Domain.Exceptions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventForge.Events.Application.Services;

/// <summary>
/// Сервис обработки событий
/// </summary>
public class EventService(IEventRepository repository, ILogger<EventService> logger, ICacheService cache, IOptions<RedisOptions> redisOptions, TimeProvider timeProvider) : IEventService
{
    // Один семафор на уникальный ключ (ConcurrentDictionary), максимум 1 поток в критической секции
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();


    public async Task<EventDTO> CreateEventAsync(CreateEventDto newEventDTO, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Создание нового события: {Title}", newEventDTO.Title);
        var newEvent = Event.Create(
            newEventDTO.Title,
            newEventDTO.StartAt,
            newEventDTO.EndAt,
            newEventDTO.TotalSeats,
            newEventDTO.Description
        );

        await repository.AddAsync(newEvent, ct);
        logger.LogInformation("Событие успешно создано. ID: {Id}", newEvent.Id);
        return newEvent.ToDto();
    }

    public async Task CancelEventAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogDebug("Попытка удаления события с ID: {Id}", eventId);
        if (!await repository.DeleteAsync(eventId, ct))
        {
            throw new NotFoundException(nameof(Event), eventId.ToString());
        }

        await cache.RemoveAsync(KeysForEvents.ForEvent(eventId));

        logger.LogInformation("Событие успешно удалено. ID: {Id} ", eventId);
    }

    public async Task<PaginatedResultDTO> GetEventsAsync(EventsFilterDTO filter, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Запрос списка событий в {Now}. Фильтр: {Filter}", timeProvider.GetUtcNow().UtcDateTime, filter.Title);

        var result = await repository.GetPagedAsync(filter.Title, filter.From, filter.To, filter.Page, filter.PageSize, ct);
        var items = result.Items.Select(r => r.ToDto()).ToList();

        return new PaginatedResultDTO(result.TotalCount, items, filter.Page, filter.PageSize);
    }


    public async Task<EventDTO> GetEventAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var eventDto = await GetOrSetCacheAsync(
            KeysForEvents.ForEvent(eventId),
            async () =>
            {
                var existedEvent = await repository.GetByIdAsync(eventId, ct);
                if (existedEvent == null)
                    throw new NotFoundException(nameof(Event), eventId.ToString());
                return existedEvent.ToDto();
            },
            TimeSpan.FromMinutes(redisOptions.Value.SingleEventExpirationMinutes),
            ct);

        return eventDto;
    }

    public async Task ChangeEventAsync(Guid eventId, UpdateEventDto currentEvent, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Обновление события {Id}", eventId);
        var existedEvent = await repository.GetByIdAsync(eventId, ct);
        if (existedEvent == null)
        {
            logger.LogError("Ошибка обновления: событие не существует. ID: {Id}", eventId);
            throw new NotFoundException(nameof(Event), eventId.ToString(), "Событие с таким ID не найдено");
        }

        existedEvent.UpdateEvent(
            currentEvent.Title ?? existedEvent.Title,
            currentEvent.StartAt ?? existedEvent.StartAt,
            currentEvent.EndAt ?? existedEvent.EndAt,
            currentEvent.Description ?? existedEvent.Description);

        await repository.UpdateAsync(existedEvent, ct);
        await cache.RemoveAsync(KeysForEvents.ForEvent(eventId));

        logger.LogInformation("Событие успешно обновлено. ID: {Id}", eventId);
    }


    public async Task ReleaseSeatAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var existedEvent = await repository.GetByIdAsync(eventId, ct);
        if (existedEvent == null)
            throw new NotFoundException(nameof(Event), eventId.ToString());

        existedEvent.ReleaseSeats();
        await repository.UpdateAsync(existedEvent, ct);
        await cache.RemoveAsync(KeysForEvents.ForEvent(eventId));
        await cache.RemoveAsync(KeysForEvents.TopEvents);
    }

    public async Task<PaginatedResultTop10DTO> GetTop10EventsAsync(CancellationToken ct)
    {

        ct.ThrowIfCancellationRequested();

        var eventDto = await GetOrSetCacheAsync(
            KeysForEvents.TopEvents,
            async () =>
            {
                var result = await repository.GetTop10EventsAsync(ct);
                var items = result.Items.Select(r => r.ToDto()).ToList();
                return new PaginatedResultTop10DTO(items);
            },
            TimeSpan.FromMinutes(redisOptions.Value.TopEventsExpirationMinutes),
            ct);

        return eventDto;
    }


    /// <summary>
    /// Получение данных из кэша или установка их при отсутствии
    /// </summary>
    private async Task<T?> GetOrSetCacheAsync<T>(
        string cacheKey,
        Func<Task<T>> dbQuery,
        TimeSpan expiration,
        CancellationToken ct)
    {
        // Быстрая проверка кэша (без блокировки)
        var cached = await cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
            return JsonSerializer.Deserialize<T>(cached);

        // Захватываем блокировку на этот ключ
        var semaphore = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        try
        {
            // Double-check: возможно другой поток уже заполнил кэш
            cached = await cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
                return JsonSerializer.Deserialize<T>(cached);

            // Только ОДИН поток идёт в БД
            var result = await dbQuery();
            var serialized = JsonSerializer.Serialize(result);
            await cache.SetStringAsync(cacheKey, serialized, expiration);
            return result;
        }
        finally
        {
            semaphore.Release();
            if (semaphore.CurrentCount == 1)
            {
                _locks.TryRemove(cacheKey, out _);
            }
        }
    }


}

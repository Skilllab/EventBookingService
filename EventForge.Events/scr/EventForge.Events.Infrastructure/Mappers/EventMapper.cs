using EventForge.Events.Domain.Entities;
using EventForge.Events.Infrastructure.Entities;

namespace EventForge.Events.Infrastructure.Mappers;

/// <summary>
/// Маппер событий между доменной моделью и сущностью БД
/// </summary>
public static class EventMapper
{
    /// <summary>
    /// Маппер в доменную модель из сущности БД
    /// </summary>
    /// <param name="entity">Сущность из БД</param>
    public static Event ToDomain(this EventEntity entity)
    {
        var domain = Event.Create(
            entity.Title,
            entity.StartAt,
            entity.EndAt,
            entity.TotalSeats,
            entity.Description);

        // Чтобы синхронизировать Id и остаток мест из БД:
        typeof(Event).GetProperty(nameof(Event.Id))?.SetValue(domain, entity.Id);
        typeof(Event).GetProperty(nameof(Event.AvailableSeats))?.SetValue(domain, entity.AvailableSeats);

        return domain;
    }

    /// <summary>
    /// Маппер в сущность БД из доменной модели
    /// </summary>
    /// <param name="domain">Доменная модель</param>
    /// <returns></returns>
    public static EventEntity ToEntity(this Event domain) =>
        new()
        {
            Id = domain.Id,
            Title = domain.Title,
            Description = domain.Description,
            StartAt = domain.StartAt,
            EndAt = domain.EndAt,
            TotalSeats = domain.TotalSeats,
            AvailableSeats = domain.AvailableSeats,
        };
}
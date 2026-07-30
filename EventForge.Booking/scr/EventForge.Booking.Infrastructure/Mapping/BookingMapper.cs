using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Infrastructure.Entities;

namespace EventForge.Booking.Infrastructure.Mapping;

/// <summary>
/// Маппер между доменной моделью и сущностью базы данных
/// </summary>
public static class BookingMapper
{
    /// <summary>
    /// Из сущности БД в доменную модель
    /// </summary>
    /// <param name="entity">Сущность базы данных</param>
    public static BookingModel ToDomain(this BookingEntity entity)
    {
        var domain = BookingModel.Create(entity.EventId, entity.UserId, entity.CreatedAt);

        var type = typeof(BookingModel);
        type.GetProperty(nameof(BookingModel.Id))?.SetValue(domain, entity.Id);
        type.GetProperty(nameof(BookingModel.UserId))?.SetValue(domain, entity.UserId);

        type.GetProperty(nameof(BookingModel.Status))?.SetValue(domain, Enum.Parse<BookingStatus>(entity.Status));
        domain.ProcessedAt = entity.ProcessedAt;

        return domain;
    }

    /// <summary>
    /// Из домена в сущность БД
    /// </summary>
    /// <param name="domain">Доменная модель</param>
    public static BookingEntity ToEntity(this BookingModel domain) =>
        new()
        {
            Id = domain.Id,
            EventId = domain.EventId,
            Status = domain.Status.ToString(),
            CreatedAt = domain.CreatedAt,
            ProcessedAt = domain.ProcessedAt,
            UserId = domain.UserId
        };

    /// <summary>
    /// Обновление сущности БД на основе доменной модели
    /// </summary>
    /// <param name="domain">Доменная модель</param>
    /// <param name="entity">Сущность базы данных</param>
    public static void UpdateEntity(this BookingModel domain, BookingEntity entity)
    {
        entity.Status = domain.Status.ToString();
        entity.ProcessedAt = domain.ProcessedAt;
    }
}

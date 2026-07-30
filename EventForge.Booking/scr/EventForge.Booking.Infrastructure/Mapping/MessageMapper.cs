using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Infrastructure.Entities;

namespace EventForge.Booking.Infrastructure.Mapping;

/// <summary>
/// Маппер между доменной моделью и сущностью базы данных
/// </summary>
public static class MessageMapper
{
    /// <summary>
    /// Из сущности БД в доменную модель
    /// </summary>
    public static OutboxMessage ToDomain(this OutboxMessageEntity entity) =>
        OutboxMessage.Restore(
            entity.Id,
            entity.Type,
            entity.Topic,
            entity.MessageKey,
            entity.Payload,
            entity.CreatedAt,
            entity.ProcessedAt,
            entity.Error,
            entity.TraceParent,
            entity.TraceState);

    /// <summary>
    /// Из домена в сущность БД
    /// </summary>
    public static OutboxMessageEntity ToEntity(this OutboxMessage domain) =>
        new()
        {
            Id = domain.Id,
            Type = domain.Type,
            Topic = domain.Topic,
            MessageKey = domain.MessageKey,
            Payload = domain.Payload,
            CreatedAt = domain.CreatedAt,
            ProcessedAt = domain.ProcessedAt,
            Error = domain.Error,
            TraceParent = domain.TraceParent,
            TraceState = domain.TraceState
        };
}

namespace EventForge.Booking.Infrastructure.Entities;

/// <summary>
/// Сущность бронирования для БД
/// </summary>
public class BookingEntity
{
    /// <summary>
    /// Уникальный идентификатор брони
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя, создавшего бронирование
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Идентификатор события, к которому относится бронь
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Текущий статус брони
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Дата и время создания брони
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время обработки брони
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}

namespace EventForge.Booking.Domain.Entities;

/// <summary>
/// Модель бронирования события
/// </summary>
public class BookingModel
{
    /// <summary>
    /// Уникальный идентификатор брони
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Идентификатор события, к которому относится бронь
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Идентификатор пользователя, который создал бронь
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Текущий статус брони
    /// </summary>
    public BookingStatus Status { get; private set; }

    /// <summary>
    /// Дата и время создания брони
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Дата и время обработки брони
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    private BookingModel(Guid eventId, Guid userId, DateTime createdAt)
    {
        Id = Guid.NewGuid();
        Status = BookingStatus.Pending;
        CreatedAt = createdAt;
        EventId = eventId;
        UserId = userId;
    }

    /// <summary>
    /// Подтверждаем бронирование
    /// </summary>
    /// <param name="processedAt">Время работы с бронированием</param>
    public void Confirm(DateTime processedAt)
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = processedAt;
    }

    /// <summary>
    /// Отменяем бронирование
    /// </summary>
    /// <param name="processedAt">Время работы с бронированием</param>
    public void Reject(DateTime processedAt)
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = processedAt;
    }

    public void Cancel(DateTime processedAt)
    {
        Status = BookingStatus.Cancelled;
        ProcessedAt = processedAt;
    }

    /// <summary>
    /// Метод создания события
    /// </summary>
    /// <param name="eventId">Идентификатор привязанного события</param>
    /// <param name="createdAt">Время создания брони. Может быть разное в зависимости от региона</param>
    /// <returns></returns>
    public static BookingModel Create(Guid eventId, Guid userId, DateTime createdAt) => new(eventId, userId, createdAt);
}

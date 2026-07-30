namespace EventForge.Events.Infrastructure.Entities;

/// <summary>
/// EF-сущность события
/// </summary>
public class EventEntity
{
    /// <summary>
    /// Идентификатор события
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Название события
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Дата начала
    /// </summary>
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата окончания
    /// </summary>
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Общее количество мест
    /// </summary>
    public int TotalSeats { get; set; }

    /// <summary>
    /// Количество свободных мест
    /// </summary>
    public int AvailableSeats { get; set; }
}

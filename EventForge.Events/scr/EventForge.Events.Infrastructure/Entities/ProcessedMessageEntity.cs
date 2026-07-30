namespace EventForge.Events.Infrastructure.Entities;

/// <summary>
/// Запись о сообщении, которое уже было обработано consumer-ом
/// </summary>
public class ProcessedMessageEntity
{
    /// <summary>
    /// Идентификатор сообщения
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Тип обработанного сообщения
    /// </summary>
    public required string MessageType { get; set; }

    /// <summary>
    /// Время обработки
    /// </summary>
    public DateTime ProcessedAt { get; set; }
}

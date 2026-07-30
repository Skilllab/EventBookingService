namespace EventForge.Events.Infrastructure.Entities;

/// <summary>
/// Запись outbox для гарантированной последующей публикации сообщения
/// </summary>
public class OutboxMessageEntity
{
    /// <summary>
    /// Уникальный идентификатор outbox-сообщения
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Тип сообщения
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Имя топика Kafka
    /// </summary>
    public required string Topic { get; set; }

    /// <summary>
    /// Ключ сообщения Kafka
    /// </summary>
    public required string MessageKey { get; set; }

    /// <summary>
    /// JSON-представление события
    /// </summary>
    public required string Payload { get; set; }

    /// <summary>
    /// Время создания записи
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Время успешной публикации
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Последняя ошибка публикации
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Traceparent для трассировки
    /// </summary>
    public string? TraceParent { get; set; }

    /// <summary>
    /// Tracestate для трассировки
    /// </summary>
    public string? TraceState { get; set; }

}

namespace EventForge.Events.Infrastructure.Entities;

/// <summary>
/// Настройки подключения к Kafka
/// </summary>
public class KafkaOptions
{
    /// <summary>
    /// Адрес Kafka bootstrap servers
    /// </summary>
    public string BootstrapServers { get; set; } = string.Empty;

    /// <summary>
    /// Имя consumer group
    /// </summary>
    public string ConsumerGroup { get; set; } = string.Empty;


    /// <summary>
    /// Количество быстрых ретраев на месте
    /// </summary>
    public int InPlaceRetryCount { get; set; } = 3;

    /// <summary>
    /// Базовая задержка быстрых ретраев (мс), далее экспоненциально
    /// </summary>
    public int InPlaceRetryBaseDelayMs { get; set; } = 200;

    /// <summary>
    /// Максимальное число попыток в retry topic
    /// </summary>
    public int RetryTopicMaxAttempts { get; set; } = 5;

    /// <summary>
    /// Начальная задержка retry topic (сек)
    /// </summary>
    public int RetryTopicInitialDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Потолок задержки retry topic (сек)
    /// </summary>
    public int RetryTopicMaxDelaySeconds { get; set; } = 900;

}

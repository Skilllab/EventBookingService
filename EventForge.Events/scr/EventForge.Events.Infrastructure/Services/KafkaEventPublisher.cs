using Confluent.Kafka;

using EventForge.Events.Application.Interfaces;
using EventForge.Events.Infrastructure.Entities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventForge.Events.Infrastructure.Services;

/// <summary>
/// Kafka publisher события BookingConfirmed
/// </summary>
public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    //Для тестовых целей, чтобы проверить, что событие отправляется в кафку
    public KafkaEventPublisher(
        IProducer<string, string> producer,
        ILogger<KafkaEventPublisher> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task PublishRawAsync(string topic, string key, string payload, CancellationToken ct)
    {
        var headers = new Headers();
        KafkaTraceContext.InjectCurrentContext(headers);

        await _producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = key,
                Value = payload
            },
            ct);

        _logger.LogInformation("Сообщение опубликовано в Kafka. Topic={Topic}, Key={Key}", topic, key);
    }

    /// <summary>
    /// Диспоузим продюсер
    /// </summary>
    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}

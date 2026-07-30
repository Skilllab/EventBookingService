using Confluent.Kafka;

using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Infrastructure.Entities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventForge.Booking.Infrastructure.Services;

/// <summary>
/// Kafka publisher
/// </summary>
public sealed class KafkaBookingPublisher : IBookingPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaBookingPublisher> _logger;

    public KafkaBookingPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaBookingPublisher> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    //Для тестовых целей, чтобы проверить, что событие отправляется в кафку
    public KafkaBookingPublisher(
        IProducer<string, string> producer,
        ILogger<KafkaBookingPublisher> logger)
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

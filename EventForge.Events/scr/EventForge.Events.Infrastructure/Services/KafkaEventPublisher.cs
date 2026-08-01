using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
    private readonly KafkaMetrics _metrics;


    public KafkaEventPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaEventPublisher> logger,
        KafkaMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    //Для тестовых целей, чтобы проверить, что событие отправляется в кафку
    public KafkaEventPublisher(
        IProducer<string, string> producer,
        ILogger<KafkaEventPublisher> logger, KafkaMetrics metrics)
    {
        _producer = producer;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task PublishRawAsync(string topic, string key, string payload, CancellationToken ct)
    {
        var headers = new Headers();
        KafkaTraceContext.InjectCurrentContext(headers);

        var sw = Stopwatch.StartNew();

        await _producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = key,
                Value = payload
            },
            ct);

        sw.Stop();
        // RED: длительность
        _metrics.PublishDuration.Record(sw.Elapsed.TotalSeconds);
        // RED: сообщение опубликовано
        _metrics.PublishedMessages.Add(1);                         


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

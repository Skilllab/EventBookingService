using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using EventForge.Booking.Application.Interfaces;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventForge.Booking.Infrastructure.Services;

/// <summary>
/// Фоновый сервис, который читает outbox и публикует сообщения в Kafka
/// </summary>
public class OutboxPublisherBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisherBackgroundService> logger,
    TimeProvider timeProvider,
    KafkaMetrics metrics) : BackgroundService
{
    private const int BatchSize = 50;
    private const int DelaySeconds = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка фоновой публикации outbox");
            }

            await Task.Delay(TimeSpan.FromSeconds(DelaySeconds), timeProvider, stoppingToken);
        }
    }

    public async Task ProcessOnceAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IBookingPublisher>();

        var messages = await outboxRepository.GetPendingAsync(BatchSize, stoppingToken);

        foreach (var message in messages)
        {
            var parent = KafkaTraceContext.ExtractFromOutbox(message.TraceParent, message.TraceState);
            using var activity = KafkaTraceContext.Source.StartActivity("kafka outbox publish", ActivityKind.Producer, parent);

            activity?.SetTag("messaging.system", "kafka");
            activity?.SetTag("messaging.destination.name", message.Topic);
            activity?.SetTag("messaging.message.id", message.Id.ToString());

            var sw = Stopwatch.StartNew();


            try
            {
                await publisher.PublishRawAsync(message.Topic, message.MessageKey, message.Payload, stoppingToken);
                await outboxRepository.MarkProcessedAsync(message.Id, stoppingToken);

                // RED: успешная публикация
                metrics.PublishedMessages.Add(1);
                sw.Stop();
                metrics.PublishDuration.Record(sw.Elapsed.TotalSeconds);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                await outboxRepository.MarkFailedAsync(message.Id, ex.Message, stoppingToken);

                // RED: ошибка публикации
                metrics.PublishErrors.Add(1);

                logger.LogError(ex, "Ошибка публикации outbox сообщения {MessageId}", message.Id);
            }
        }
    }
}

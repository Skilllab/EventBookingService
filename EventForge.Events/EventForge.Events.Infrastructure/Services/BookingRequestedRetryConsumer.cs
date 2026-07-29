using System.Diagnostics;
using System.Text.Json;

using Confluent.Kafka;

using EventForge.Contract.Brokers;
using EventForge.Events.Application.Interfaces;
using EventForge.Events.Domain.Entities;
using EventForge.Events.Infrastructure.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventForge.Events.Infrastructure.Services
{
    /// <summary>
    /// Потребитель retry-сообщений booking-requested-retry.
    /// Использует RetryTopicMaxAttempts для решения: повтор или DLQ.
    /// </summary>
    public sealed class BookingRequestedRetryConsumer(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<BookingRequestedRetryConsumer> logger,
        BookingRequestedMessageProcessor messageProcessor,
        BookingRequestedDbRetryPolicy dbRetryPolicy,
        TimeProvider timeProvider) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = kafkaOptions.Value.BootstrapServers,
                GroupId = $"{kafkaOptions.Value.ConsumerGroup}-retry",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(TopicNames.BookingRequestedRetry);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    if (consumeResult?.Message?.Value == null)
                        continue;

                    var parent = KafkaTraceContext.ExtractFromHeaders(consumeResult.Message.Headers);
                    using var activity = KafkaTraceContext.Source.StartActivity("kafka consume booking-requested-retry", ActivityKind.Consumer, parent);

                    activity?.SetTag("messaging.system", "kafka");
                    activity?.SetTag("messaging.destination.name", TopicNames.BookingRequestedRetry);
                    activity?.SetTag("messaging.kafka.message_key", consumeResult.Message.Key);

                    var shouldCommit = await ProcessRetryPayloadAsync(consumeResult.Message.Value, stoppingToken);
                    if (shouldCommit)
                        consumer.Commit(consumeResult);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при обработке сообщения BookingRequestedRetry");
                }
            }
        }

        public async Task<bool> ProcessRetryPayloadAsync(string rawPayload, CancellationToken ct)
        {
            BookingRequestedRetryEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<BookingRequestedRetryEnvelope>(rawPayload);
            }
            catch (JsonException ex)
            {
                await PublishDlqAsync(rawPayload, null, TopicNames.BookingRequestedRetry, 0, ex.Message, ct);
                return true;
            }

            if (envelope is null)
            {
                await PublishDlqAsync(rawPayload, null, TopicNames.BookingRequestedRetry, 0, "BookingRequestedRetryEnvelope is null", ct);
                return true;
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (envelope.NextAttemptAtUtc > now)
            {
                await Task.Delay(envelope.NextAttemptAtUtc - now, timeProvider, ct);
            }

            try
            {
                await dbRetryPolicy.ExecuteAsync(
                    async token => await messageProcessor.ProcessAsync(envelope.Message, token),
                    ct);

                return true;
            }
            catch (DbUpdateException ex)
            {
                var maxAttempts = Math.Max(1, kafkaOptions.Value.RetryTopicMaxAttempts);

                if (envelope.RetryAttempt >= maxAttempts)
                {
                    await PublishDlqAsync(
                        envelope.RawPayload,
                        envelope.Message.MessageId,
                        TopicNames.BookingRequestedRetry,
                        envelope.RetryAttempt,
                        $"RetryTopicMaxAttempts exceeded. {ex.Message}",
                        ct);

                    return true;
                }

                var nextAttempt = envelope.RetryAttempt + 1;
                var nextEnvelope = envelope with
                {
                    RetryAttempt = nextAttempt,
                    LastError = ex.Message,
                    NextAttemptAtUtc = timeProvider.GetUtcNow().UtcDateTime.Add(CalculateRetryTopicDelay(nextAttempt))
                };

                await PublishRetryAsync(nextEnvelope, ct);
                return true;
            }
            catch (Exception ex)
            {
                await PublishDlqAsync(
                    envelope.RawPayload,
                    envelope.Message.MessageId,
                    TopicNames.BookingRequestedRetry,
                    envelope.RetryAttempt,
                    ex.Message,
                    ct);

                return true;
            }
        }

      

        private TimeSpan CalculateRetryTopicDelay(int retryAttempt)
        {
            var initialSeconds = Math.Max(1, kafkaOptions.Value.RetryTopicInitialDelaySeconds);
            var maxSeconds = Math.Max(initialSeconds, kafkaOptions.Value.RetryTopicMaxDelaySeconds);

            var seconds = initialSeconds * Math.Pow(2, retryAttempt - 1);
            return TimeSpan.FromSeconds(Math.Min(seconds, maxSeconds));
        }

        private async Task PublishRetryAsync(BookingRequestedRetryEnvelope envelope, CancellationToken ct)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            var now = timeProvider.GetUtcNow().UtcDateTime;

            var outbox = OutboxMessage.Create(
                nameof(BookingRequestedRetryEnvelope),
                TopicNames.BookingRequestedRetry,
                envelope.Message.EventId.ToString(),
                JsonSerializer.Serialize(envelope),
                now,
                null);

            await eventRepository.AddOutboxAsync(outbox, ct);
        }

        private async Task PublishDlqAsync(
            string rawPayload,
            Guid? originalMessageId,
            string sourceTopic,
            int retryAttempt,
            string error,
            CancellationToken ct)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            var now = timeProvider.GetUtcNow().UtcDateTime;

            var dlq = new BookingRequestedDlqMessage(
                Guid.NewGuid(),
                sourceTopic,
                rawPayload,
                error,
                now,
                retryAttempt,
                originalMessageId);

            var outbox = OutboxMessage.Create(
                nameof(BookingRequestedDlqMessage),
                TopicNames.BookingRequestedDlq,
                (originalMessageId ?? Guid.NewGuid()).ToString(),
                JsonSerializer.Serialize(dlq),
                now,
                null);

            await eventRepository.AddOutboxAsync(outbox, ct);
        }
    }
}

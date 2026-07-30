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

namespace EventForge.Events.Infrastructure.Services;

/// <summary>
/// Потребитель сообщений о запросах на бронирование
/// </summary>
/// <param name="scopeFactory">Фабрика сервисов</param>
/// <param name="kafkaOptions">Настройки Kafka</param>
/// <param name="logger">Логгер</param>
/// <param name="messageProcessor">Процессор сообщений</param>
/// <param name="timeProvider">Поставщик времени</param>
public class BookingRequestedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<BookingRequestedConsumer> logger,
    BookingRequestedMessageProcessor messageProcessor,
    BookingRequestedDbRetryPolicy dbRetryPolicy,
    TimeProvider timeProvider) : BackgroundService
{
    /// <summary>
    /// Обработка сообщений о запросах на бронирование
    /// </summary>
    /// <param name="message">Сообщение о запросе на бронирование</param>
    /// <param name="stoppingToken">Токен отмены</param>
    public Task HandleMessageAsync(BookingRequested? message, CancellationToken stoppingToken) =>
        messageProcessor.ProcessAsync(message, stoppingToken);

    /// <summary>
    /// Основной метод выполнения фоновой службы
    /// </summary>
    /// <param name="stoppingToken">Токен отмены</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServers,
            GroupId = kafkaOptions.Value.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(TopicNames.BookingRequested);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null)
                    continue;

                var parent = KafkaTraceContext.ExtractFromHeaders(consumeResult.Message.Headers);
                using var activity = KafkaTraceContext.Source.StartActivity("kafka consume booking-requested", ActivityKind.Consumer, parent);

                activity?.SetTag("messaging.system", "kafka");
                activity?.SetTag("messaging.destination.name", TopicNames.BookingRequested);
                activity?.SetTag("messaging.kafka.message_key", consumeResult.Message.Key);

                var shouldCommit = await ProcessPrimaryPayloadAsync(consumeResult.Message.Value, stoppingToken);
                if (shouldCommit)
                    consumer.Commit(consumeResult);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обработке сообщения BookingRequested");
            }
        }
    }

    /// <summary>
    /// Обработка основного полезного сообщения.
    /// Обрабатываем попытками с повторной отправкой в случае ошибок.
    /// </summary>
    /// <param name="rawPayload">Сырые данные сообщения</param>
    /// <param name="ct">Токен отмены</param>
    public async Task<bool> ProcessPrimaryPayloadAsync(string rawPayload, CancellationToken ct)
    {
        BookingRequested? message;
        try
        {
            message = JsonSerializer.Deserialize<BookingRequested>(rawPayload);
        }
        catch (JsonException ex)
        {
            await PublishDlqAsync(rawPayload, null, TopicNames.BookingRequested, 0, ex.Message, ct);
            return true;
        }

        if (message is null)
        {
            await PublishDlqAsync(rawPayload, null, TopicNames.BookingRequested, 0, "BookingRequested is null", ct);
            return true;
        }

        try
        {
            await dbRetryPolicy.ExecuteAsync(
                async token => await HandleMessageAsync(message, token),
                ct);

            return true;
        }
        catch (DbUpdateException ex)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            var retryEnvelope = new BookingRequestedRetryEnvelope(
                message,
                RetryAttempt: 1,
                FirstFailedAtUtc: now,
                NextAttemptAtUtc: now.Add(CalculateRetryTopicDelay(1)),
                LastError: ex.Message,
                RawPayload: rawPayload);

            await PublishRetryAsync(retryEnvelope, ct);
            return true;
        }
        catch (Exception ex)
        {
            await PublishDlqAsync(rawPayload, message.MessageId, TopicNames.BookingRequested, 0, ex.Message, ct);
            return true;
        }
    }
   

    /// <summary>
    /// Вычисление задержки для повторной попытки обработки сообщения с использованием экспоненциального backoff
    /// </summary>
    /// <param name="retryAttempt">Номер попытки</param>
    /// <returns>Время задержки перед следующей попыткой</returns>
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

    /// <summary>
    /// Публикация сообщения в Dead Letter Queue для последующего анализа и обработки
    /// </summary>
    /// <param name="rawPayload">Сырые данные сообщения</param>
    /// <param name="originalMessageId">Идентификатор оригинального сообщения</param>
    /// <param name="sourceTopic">Топик источника</param>
    /// <param name="retryAttempt">Попытка повторной отправки</param>
    /// <param name="error">Сообщение об ошибке</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns></returns>
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

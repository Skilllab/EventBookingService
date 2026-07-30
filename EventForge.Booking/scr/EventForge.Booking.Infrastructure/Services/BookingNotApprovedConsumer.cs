using System.Diagnostics;
using System.Text.Json;

using Confluent.Kafka;

using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Infrastructure.Entities;
using EventForge.Contract.Brokers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventForge.Booking.Infrastructure.Services;

public class BookingNotApprovedConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<BookingNotApprovedConsumer> logger) : BackgroundService
{
    
    private async Task HandleMessageAsync(
        BookingNotApproved? message, CancellationToken ct)
    {
        if (message == null)
        {
            logger.LogWarning(
                "Получено пустое или невалидное сообщение BookingRejected");
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var processedRepository = scope.ServiceProvider
            .GetRequiredService<IProcessedMessageRepository>();
        var bookingRepository = scope.ServiceProvider
            .GetRequiredService<IBookingRepository>();

        if (await processedRepository.ExistsAsync(message.MessageId, ct))
        {
            logger.LogInformation(
                "Сообщение {MessageId} уже обработано, пропускаем",
                message.MessageId);
            return;
        }

        var booking = await bookingRepository.GetByIdAsync(
            message.BookingId, ct);

        if (booking == null)
        {
            logger.LogWarning(
                "Бронь {BookingId} не найдена для сообщения {MessageId}",
                message.BookingId, message.MessageId);

            await processedRepository.AddAsync(
                message.MessageId, nameof(BookingNotApproved), ct);
            return;
        }

        if (booking.Status != BookingStatus.Pending)
        {
            logger.LogInformation(
                "Бронь {BookingId} уже в статусе {Status}, пропускаем отклонение",
                message.BookingId, booking.Status);

            await processedRepository.AddAsync(
                message.MessageId, nameof(BookingNotApproved), ct);
            return;
        }

        var rejected = await bookingRepository.RejectBookingAsync(
            message.BookingId, message.RejectedAt, ct);

        if (!rejected)
        {
            logger.LogWarning(
                "Не удалось отклонить бронь {BookingId}",
                message.BookingId);

            await processedRepository.AddAsync(
                message.MessageId, nameof(BookingNotApproved), ct);
            return;
        }

        await processedRepository.AddAsync(
            message.MessageId, nameof(BookingNotApproved), ct);

        logger.LogInformation(
            "Бронь {BookingId} отклонена. MessageId={MessageId}",
            message.BookingId, message.MessageId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServers,
            GroupId = kafkaOptions.Value.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(TopicNames.BookingNotApproved);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null)
                    continue;

                var parent = KafkaTraceContext.ExtractFromHeaders(consumeResult.Message.Headers);
                using var activity = KafkaTraceContext.Source.StartActivity("kafka consume booking-not-approved", ActivityKind.Consumer, parent);

                activity?.SetTag("messaging.system", "kafka");
                activity?.SetTag("messaging.destination.name", TopicNames.BookingNotApproved);
                activity?.SetTag("messaging.kafka.message_key", consumeResult.Message.Key);

                var message = JsonSerializer.Deserialize<BookingNotApproved>(consumeResult.Message.Value);
                await HandleMessageAsync(message, stoppingToken);

                consumer.Commit(consumeResult);

            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Ошибка при обработке сообщения BookingNotApproved");
            }
        }
    }
}

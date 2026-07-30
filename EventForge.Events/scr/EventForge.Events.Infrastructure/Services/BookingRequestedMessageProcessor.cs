using System.Text.Json;

using EventForge.CacheKeys;
using EventForge.Contract.Brokers;
using EventForge.Contract.Enums;
using EventForge.Events.Application.Interfaces;
using EventForge.Events.Domain.Entities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventForge.Events.Infrastructure.Services
{
    /// <summary>
    /// Процессор сообщений о запросах на бронирование
    /// </summary>
    /// <param name="scopeFactory">Фабрика сервисов</param>
    /// <param name="cache">Сервис кэша</param>
    /// <param name="timeProvider">Поставщик времени</param>
    /// <param name="logger">Логгер</param>
    public sealed class BookingRequestedMessageProcessor(
        IServiceScopeFactory scopeFactory,
        ICacheService cache,
        TimeProvider timeProvider,
        ILogger<BookingRequestedMessageProcessor> logger)
    {
        /// <summary>
        /// Обработка сообщения о запросе на бронирование
        /// </summary>
        /// <param name="message"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public async Task ProcessAsync(BookingRequested? message, CancellationToken stoppingToken)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            if (message == null)
            {
                logger.LogWarning("Получено пустое или невалидное сообщение BookingRequested");
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var processedRepository =
                scope.ServiceProvider.GetRequiredService<IProcessedMessageRepository>();
            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

            if (await processedRepository.ExistsAsync(message.MessageId, stoppingToken))
                return;

            var bookingEvent = await eventRepository.GetByIdAsync(message.EventId, stoppingToken);
            if (bookingEvent == null)
            {
                var rejected = new BookingRejected(Guid.NewGuid(), message.BookingId,
                    message.EventId, message.UserId, now, "не найдено событие");
                var outboxRejected = OutboxMessage.Create(nameof(BookingRejected),
                    TopicNames.BookingRejected, message.EventId.ToString(),
                    JsonSerializer.Serialize(rejected), now, null);

                await eventRepository.AddOutboxAsync(outboxRejected, stoppingToken);
                await processedRepository.AddAsync(message.MessageId, nameof(BookingRejected),
                    stoppingToken);
                return;
            }

            if (bookingEvent.StartAt <= now)
            {
                var notApproved = new BookingNotApproved(Guid.NewGuid(), message.BookingId,
                    message.EventId, message.UserId, now, BookingNotApprovedReason.EventStarted);
                var outboxNotApproved = OutboxMessage.Create(nameof(BookingNotApproved),
                    TopicNames.BookingNotApproved, message.EventId.ToString(),
                    JsonSerializer.Serialize(notApproved), now, null);

                await eventRepository.AddOutboxAsync(outboxNotApproved, stoppingToken);
                await processedRepository.AddAsync(message.MessageId, nameof(BookingNotApproved),
                    stoppingToken);
                return;
            }

            if (!bookingEvent.TryReserveSeats(message.SeatsCount))
            {
                var notApproved = new BookingNotApproved(Guid.NewGuid(), message.BookingId,
                    message.EventId, message.UserId, now, BookingNotApprovedReason.NoSeats);
                var outboxNotApproved = OutboxMessage.Create(nameof(BookingNotApproved),
                    TopicNames.BookingNotApproved, message.EventId.ToString(),
                    JsonSerializer.Serialize(notApproved), now, null);

                await eventRepository.AddOutboxAsync(outboxNotApproved, stoppingToken);
                await processedRepository.AddAsync(message.MessageId, nameof(BookingNotApproved),
                    stoppingToken);
                return;
            }

            var confirmed = new BookingConfirmed(Guid.NewGuid(), message.BookingId, message.EventId,
                message.UserId, message.SeatsCount, now);
            var outboxConfirmed = OutboxMessage.Create(nameof(BookingConfirmed),
                TopicNames.BookingConfirmed, message.EventId.ToString(),
                JsonSerializer.Serialize(confirmed), now, null);

            await eventRepository.SaveEventAndOutboxAsync(bookingEvent, outboxConfirmed,
                stoppingToken);
            await processedRepository.AddAsync(message.MessageId, nameof(BookingConfirmed),
                stoppingToken);

            await cache.RemoveAsync(KeysForEvents.ForEvent(message.EventId));
            await cache.RemoveAsync(KeysForEvents.TopEvents);
        }
    }
}

using System.Text.Json;

using EventForge.Contract.Brokers;
using EventForge.Events.Application.Interfaces;
using EventForge.Events.Domain.Entities;
using EventForge.Events.Infrastructure.Entities;
using EventForge.Events.Infrastructure.Services;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Moq;

namespace EventForge.Events.UnitTests
{
    public class BookingRequestedRetryConsumerTests
    {

        [Fact]
        public async Task ProcessRetryPayloadAsync_Should_Send_Dlq_When_MaxAttempts_Reached()
        {
            var services = new ServiceCollection();
            var eventRepositoryMock = new Mock<IEventRepository>();
            var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
            var cacheMock = new Mock<ICacheService>();
            var time = new FakeTimeProvider(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));

            processedRepositoryMock
                .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("db transient"));

            OutboxMessage? captured = null;
            eventRepositoryMock
                .Setup(x => x.AddOutboxAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
                .Callback<OutboxMessage, CancellationToken>((m, _) => captured = m)
                .Returns(Task.CompletedTask);

            services.AddSingleton(eventRepositoryMock.Object);
            services.AddSingleton(processedRepositoryMock.Object);

            await using var provider = services.BuildServiceProvider();

            var processor = new BookingRequestedMessageProcessor(
                provider.GetRequiredService<IServiceScopeFactory>(),
                cacheMock.Object,
                time,
                Mock.Of<ILogger<BookingRequestedMessageProcessor>>());

            var retryPolicy = new BookingRequestedDbRetryPolicy(
                Options.Create(new KafkaOptions
                {
                    InPlaceRetryCount = 0,
                    InPlaceRetryBaseDelayMs = 1
                }),
                Mock.Of<ILogger<BookingRequestedDbRetryPolicy>>());

            using var sut = new BookingRequestedRetryConsumer(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(new KafkaOptions
                {
                    InPlaceRetryCount = 0,
                    RetryTopicMaxAttempts = 2,
                    RetryTopicInitialDelaySeconds = 1,
                    RetryTopicMaxDelaySeconds = 10
                }),
                Mock.Of<ILogger<BookingRequestedRetryConsumer>>(),
                processor,
                retryPolicy,
                time);

            var message = new BookingRequested(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, time.GetUtcNow().UtcDateTime);
            var envelope = new BookingRequestedRetryEnvelope(
                message,
                RetryAttempt: 2,
                FirstFailedAtUtc: time.GetUtcNow().UtcDateTime,
                NextAttemptAtUtc: time.GetUtcNow().UtcDateTime,
                LastError: "prev",
                RawPayload: JsonSerializer.Serialize(message));

            var result = await sut.ProcessRetryPayloadAsync(JsonSerializer.Serialize(envelope), CancellationToken.None);

            result.Should().BeTrue();
            captured.Should().NotBeNull();
            captured!.Topic.Should().Be(TopicNames.BookingRequestedDlq);
            captured.Type.Should().Be(nameof(BookingRequestedDlqMessage));
        }

        [Fact]
        public async Task ProcessRetryPayloadAsync_Should_Requeue_When_MaxAttempts_Not_Reached()
        {
            var services = new ServiceCollection();
            var eventRepositoryMock = new Mock<IEventRepository>();
            var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
            var cacheMock = new Mock<ICacheService>();
            var time = new FakeTimeProvider(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));

            processedRepositoryMock
                .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("db transient"));

            OutboxMessage? captured = null;
            eventRepositoryMock
                .Setup(x => x.AddOutboxAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
                .Callback<OutboxMessage, CancellationToken>((m, _) => captured = m)
                .Returns(Task.CompletedTask);

            services.AddSingleton(eventRepositoryMock.Object);
            services.AddSingleton(processedRepositoryMock.Object);

            await using var provider = services.BuildServiceProvider();

            var processor = new BookingRequestedMessageProcessor(
                provider.GetRequiredService<IServiceScopeFactory>(),
                cacheMock.Object,
                time,
                Mock.Of<ILogger<BookingRequestedMessageProcessor>>());

            var retryPolicy = new BookingRequestedDbRetryPolicy(
                Options.Create(new KafkaOptions
                {
                    InPlaceRetryCount = 0,
                    InPlaceRetryBaseDelayMs = 1
                }),
                Mock.Of<ILogger<BookingRequestedDbRetryPolicy>>());

            using var sut = new BookingRequestedRetryConsumer(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(new KafkaOptions
                {
                    InPlaceRetryCount = 0,
                    RetryTopicMaxAttempts = 2,
                    RetryTopicInitialDelaySeconds = 1,
                    RetryTopicMaxDelaySeconds = 10
                }),
                Mock.Of<ILogger<BookingRequestedRetryConsumer>>(),
                processor,
                retryPolicy,
                time);

            var message = new BookingRequested(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, time.GetUtcNow().UtcDateTime);
            var envelope = new BookingRequestedRetryEnvelope(
                message,
                RetryAttempt: 1,
                FirstFailedAtUtc: time.GetUtcNow().UtcDateTime,
                NextAttemptAtUtc: time.GetUtcNow().UtcDateTime,
                LastError: "prev",
                RawPayload: JsonSerializer.Serialize(message));

            var result = await sut.ProcessRetryPayloadAsync(JsonSerializer.Serialize(envelope), CancellationToken.None);

            result.Should().BeTrue();
            captured.Should().NotBeNull();
            captured!.Topic.Should().Be(TopicNames.BookingRequestedRetry);
            captured.Type.Should().Be(nameof(BookingRequestedRetryEnvelope));

            var next = JsonSerializer.Deserialize<BookingRequestedRetryEnvelope>(captured.Payload);
            next.Should().NotBeNull();
            next!.RetryAttempt.Should().Be(2);
        }
    }
}

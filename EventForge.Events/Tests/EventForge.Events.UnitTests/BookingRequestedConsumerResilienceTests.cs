using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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

using Xunit;

namespace EventForge.Events.UnitTests
{
    public class BookingRequestedConsumerResilienceTests
    {
        [Fact]
        public async Task ProcessPrimaryPayloadAsync_Should_Send_Dlq_When_Json_Invalid()
        {
            var services = new ServiceCollection();
            var eventRepositoryMock = new Mock<IEventRepository>();
            var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
            var cacheMock = new Mock<ICacheService>();
            var time = new FakeTimeProvider(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));

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

            using var sut = new BookingRequestedConsumer(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(new KafkaOptions()),
                Mock.Of<ILogger<BookingRequestedConsumer>>(),
                processor,
                retryPolicy,
                time);

            var result = await sut.ProcessPrimaryPayloadAsync("{ not valid json", CancellationToken.None);

            result.Should().BeTrue();
            captured.Should().NotBeNull();
            captured!.Topic.Should().Be(TopicNames.BookingRequestedDlq);
            captured.Type.Should().Be(nameof(BookingRequestedDlqMessage));
        }

        [Fact]
        public async Task ProcessPrimaryPayloadAsync_Should_Send_Retry_When_DbUpdateException_And_InPlaceExceeded()
        {
            var services = new ServiceCollection();
            var eventRepositoryMock = new Mock<IEventRepository>();
            var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
            var cacheMock = new Mock<ICacheService>();
            var time = new FakeTimeProvider(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));

            processedRepositoryMock
                .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("db is down"));

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

            using var sut = new BookingRequestedConsumer(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(new KafkaOptions()),
                Mock.Of<ILogger<BookingRequestedConsumer>>(),
                processor,
                retryPolicy,
                time);

            var msg = new BookingRequested(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2, time.GetUtcNow().UtcDateTime);
            var raw = JsonSerializer.Serialize(msg);

            var result = await sut.ProcessPrimaryPayloadAsync(raw, CancellationToken.None);

            result.Should().BeTrue();
            captured.Should().NotBeNull();
            captured!.Topic.Should().Be(TopicNames.BookingRequestedRetry);
            captured.Type.Should().Be(nameof(BookingRequestedRetryEnvelope));
        }
    }
}

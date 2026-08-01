using System;
using System.Threading;
using System.Threading.Tasks;

using EventForge.Events.Application.Interfaces;
using EventForge.Events.Domain.Entities;
using EventForge.Events.Infrastructure.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Moq;

using Xunit;

namespace EventForge.Events.UnitTests
{
    public class OutboxPublisherBackgroundServiceTests
    {
        private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));
        [Fact]
        public async Task ProcessOnceAsync_Should_Publish_And_MarkProcessed_For_Each_Pending_Message()
        {
            // Arrange
            var services = new ServiceCollection();
            var outboxRepositoryMock = new Mock<IOutboxRepository>();
            var publisherMock = new Mock<IEventPublisher>();
            var message1 = OutboxMessage.Create("Сообщение 1", "topic-1", "key1", "{}", _timeProvider.GetUtcNow().UtcDateTime, null);
            var message2 = OutboxMessage.Create("Сообщение 2", "topic-2", "key2", "{}", _timeProvider.GetUtcNow().UtcDateTime, null);

            outboxRepositoryMock
                .Setup(x => x.GetPendingAsync(50, It.IsAny<CancellationToken>()))
                .ReturnsAsync([message1, message2]);

            services.AddSingleton(outboxRepositoryMock.Object);
            services.AddSingleton(publisherMock.Object);
            await using var provider = services.BuildServiceProvider();
#pragma warning disable CA2000
            var sut = new OutboxPublisherBackgroundService(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Mock.Of<ILogger<OutboxPublisherBackgroundService>>(),
                TimeProvider.System);
#pragma warning restore CA2000

            // Act
            await sut.ProcessOnceAsync(CancellationToken.None);

            // Assert
            publisherMock.Verify(x => x.PublishRawAsync("topic-1", "key1", "{}", It.IsAny<CancellationToken>()), Times.Once);
            publisherMock.Verify(x => x.PublishRawAsync("topic-2", "key2", "{}", It.IsAny<CancellationToken>()), Times.Once);
            outboxRepositoryMock.Verify(x => x.MarkProcessedAsync(message1.Id, It.IsAny<CancellationToken>()), Times.Once);
            outboxRepositoryMock.Verify(x => x.MarkProcessedAsync(message2.Id, It.IsAny<CancellationToken>()), Times.Once);
            outboxRepositoryMock.Verify(x => x.MarkFailedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProcessOnceAsync_Should_MarkFailed_When_Publish_Throws()
        {
            // Arrange
            var services = new ServiceCollection();
            var outboxRepositoryMock = new Mock<IOutboxRepository>();
            var publisherMock = new Mock<IEventPublisher>();
            var failingMessage = OutboxMessage.Create("Сбойное сообщение", "topic", "key", "{}", _timeProvider.GetUtcNow().UtcDateTime, null);

            outboxRepositoryMock
                .Setup(x => x.GetPendingAsync(50, It.IsAny<CancellationToken>()))
                .ReturnsAsync([failingMessage]);
            publisherMock
                .Setup(x => x.PublishRawAsync("topic", "key", "{}", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Kafka недоступна"));

            services.AddSingleton(outboxRepositoryMock.Object);
            services.AddSingleton(publisherMock.Object);
            await using var provider = services.BuildServiceProvider();
#pragma warning disable CA2000
            var sut = new OutboxPublisherBackgroundService(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Mock.Of<ILogger<OutboxPublisherBackgroundService>>(),
                TimeProvider.System);
#pragma warning restore CA2000

            // Act
            await sut.ProcessOnceAsync(CancellationToken.None);

            // Assert
            publisherMock.Verify(x => x.PublishRawAsync("topic", "key", "{}", It.IsAny<CancellationToken>()), Times.Once);
            outboxRepositoryMock.Verify(x => x.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            outboxRepositoryMock.Verify(x => x.MarkFailedAsync(failingMessage.Id, "Kafka недоступна", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

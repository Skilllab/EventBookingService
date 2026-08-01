using System;
using System.Threading;
using System.Threading.Tasks;

using EventForge.Contract.Brokers;
using EventForge.Events.Application.Interfaces;
using EventForge.Events.Domain.Entities;
using EventForge.Events.Domain.Exceptions;
using EventForge.Events.Infrastructure.Entities;
using EventForge.Events.Infrastructure.Services;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Moq;

using Xunit;

namespace EventForge.Events.UnitTests;

public class KafkaConsumerTests
{
    private static BookingRequestedMessageProcessor CreateProcessor(
        ServiceProvider provider,
        FakeTimeProvider timeProvider,
        Mock<ICacheService>? cacheMock = null)
    {
        cacheMock ??= new Mock<ICacheService>();
        return new BookingRequestedMessageProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            cacheMock.Object,
            timeProvider,
            Mock.Of<ILogger<BookingRequestedMessageProcessor>>(),
            Mock.Of<KafkaMetrics>());
    }

    private static BookingRequestedDbRetryPolicy CreateDbRetryPolicy(KafkaOptions? options = null)
    {
        return new BookingRequestedDbRetryPolicy(
            Options.Create(options ?? new KafkaOptions
            {
                InPlaceRetryCount = 3,
                InPlaceRetryBaseDelayMs = 1
            }),
            Mock.Of<ILogger<BookingRequestedDbRetryPolicy>>());
    }



    [Fact]
    public async Task BookingCancelledConsumer_Should_Add_Message_After_Releasing_Seat()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var eventServiceMock = new Mock<IEventService>();
        FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));
        var message = new BookingCancelled(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, _timeProvider.GetUtcNow().UtcDateTime);

        processedRepositoryMock
            .Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        eventServiceMock
            .Setup(x => x.ReleaseSeatAsync(message.EventId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        services.AddSingleton(processedRepositoryMock.Object);
        services.AddSingleton(eventServiceMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var consumer = new BookingCancelledConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingCancelledConsumer>>(),
            Mock.Of<KafkaMetrics>());

        await consumer.HandleMessageAsync(message, CancellationToken.None);

        eventServiceMock.Verify(x => x.ReleaseSeatAsync(message.EventId, It.IsAny<CancellationToken>()), Times.Once);
        processedRepositoryMock.Verify(x => x.AddAsync(message.MessageId, nameof(BookingCancelled), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BookingCancelledConsumer_Should_Still_Mark_Message_When_Event_Not_Found()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var eventServiceMock = new Mock<IEventService>();
    FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));
    var message = new BookingCancelled(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, _timeProvider.GetUtcNow().UtcDateTime);

        processedRepositoryMock
            .Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        eventServiceMock
            .Setup(x => x.ReleaseSeatAsync(message.EventId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Event", message.EventId.ToString()));

        services.AddSingleton(processedRepositoryMock.Object);
        services.AddSingleton(eventServiceMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var consumer = new BookingCancelledConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingCancelledConsumer>>(), Mock.Of<KafkaMetrics>());

        await consumer.HandleMessageAsync(message, CancellationToken.None);

        processedRepositoryMock.Verify(x => x.AddAsync(message.MessageId, nameof(BookingCancelled), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "BookingRequested")]
    public async Task BookingRequestedConsumer_Should_Ignore_Null_Message()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));
        services.AddSingleton(processedRepositoryMock.Object);
        services.AddSingleton(eventRepositoryMock.Object);
        await using var provider = services.BuildServiceProvider();

        var processor = CreateProcessor(provider, timeProvider);
        var retryPolicy = CreateDbRetryPolicy();

        using var consumer = new BookingRequestedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingRequestedConsumer>>(),
            processor,
            retryPolicy,
            timeProvider, Mock.Of<KafkaMetrics>());

        await consumer.HandleMessageAsync(null, CancellationToken.None);

        processedRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        eventRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "BookingRequested")]
    public async Task BookingRequestedConsumer_Should_Skip_When_Already_Processed()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));
        var message = new BookingRequested(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, timeProvider.GetUtcNow().UtcDateTime);
        processedRepositoryMock
                .Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        services.AddSingleton(processedRepositoryMock.Object);
        services.AddSingleton(eventRepositoryMock.Object);
        await using var provider = services.BuildServiceProvider();
        
        var retryPolicy = CreateDbRetryPolicy();
        var processor = CreateProcessor(provider, timeProvider);

        using var consumer = new BookingRequestedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingRequestedConsumer>>(),
            processor,
            retryPolicy,
            timeProvider, Mock.Of<KafkaMetrics>());

        await consumer.HandleMessageAsync(message, CancellationToken.None);

        eventRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        processedRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "BookingRequested")]
    public async Task BookingRequestedConsumer_Should_Send_BookingRejected_When_Event_Not_Found()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));
        var message = new BookingRequested(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, timeProvider.GetUtcNow().UtcDateTime);
        processedRepositoryMock
                .Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        eventRepositoryMock
            .Setup(x => x.GetByIdAsync(message.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?) null);

        services.AddSingleton(processedRepositoryMock.Object);
        services.AddSingleton(eventRepositoryMock.Object);
        await using var provider = services.BuildServiceProvider();

        var processor = CreateProcessor(provider, timeProvider);
        var retryPolicy = CreateDbRetryPolicy();

        using var consumer = new BookingRequestedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingRequestedConsumer>>(),
            processor,
            retryPolicy,
            timeProvider, Mock.Of<KafkaMetrics>());

        await consumer.HandleMessageAsync(message, CancellationToken.None);

        // Должен добавить BookingRejected в outbox
        eventRepositoryMock.Verify(
            x => x.AddOutboxAsync(It.Is<OutboxMessage>(o => o.Type == nameof(BookingRejected)), It.IsAny<CancellationToken>()),
            Times.Once);
        processedRepositoryMock.Verify(
            x => x.AddAsync(message.MessageId, nameof(BookingRejected), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "BookingRequested")]
    public async Task BookingRequestedConsumer_Should_Send_BookingNotApproved_When_Event_Already_Started()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));

        var message = new BookingRequested(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, timeProvider.GetUtcNow().UtcDateTime);
        var pastEvent = Event.Create("Прошедшее событие", timeProvider.GetUtcNow().UtcDateTime.AddHours(-2), timeProvider.GetUtcNow().UtcDateTime.AddHours(-1), 10);

        processedRepositoryMock
            .Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        eventRepositoryMock
            .Setup(x => x.GetByIdAsync(message.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pastEvent);

        services.AddSingleton(processedRepositoryMock.Object);
        services.AddSingleton(eventRepositoryMock.Object);
        await using var provider = services.BuildServiceProvider();

        var processor = CreateProcessor(provider, timeProvider);
        var retryPolicy = CreateDbRetryPolicy();

        using var consumer = new BookingRequestedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingRequestedConsumer>>(),
            processor,
            retryPolicy,
            timeProvider, Mock.Of<KafkaMetrics>());

        await consumer.HandleMessageAsync(message, CancellationToken.None);

        eventRepositoryMock.Verify(
            x => x.AddOutboxAsync(It.Is<OutboxMessage>(o => o.Type == nameof(BookingNotApproved)), It.IsAny<CancellationToken>()),
            Times.Once);
        processedRepositoryMock.Verify(
            x => x.AddAsync(message.MessageId, nameof(BookingNotApproved), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "BookingRequested")]
    public async Task BookingRequestedConsumer_Should_Send_BookingNotApproved_When_No_Seats_Available()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));


        var message = new BookingRequested(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, timeProvider.GetUtcNow().UtcDateTime);
        // Событие в будущем, но всего 3 места
        var futureEvent = Event.Create("Future event", timeProvider.GetUtcNow().UtcDateTime.AddDays(1), timeProvider.GetUtcNow().UtcDateTime.AddDays(1).AddHours(2), 3);

        processedRepositoryMock
            .Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        eventRepositoryMock
            .Setup(x => x.GetByIdAsync(message.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(futureEvent);

        services.AddSingleton(processedRepositoryMock.Object);
        services.AddSingleton(eventRepositoryMock.Object);
        await using var provider = services.BuildServiceProvider();

        var processor = CreateProcessor(provider, timeProvider);
        var retryPolicy = CreateDbRetryPolicy();

        using var consumer = new BookingRequestedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingRequestedConsumer>>(),
            processor,
            retryPolicy,
            timeProvider, Mock.Of<KafkaMetrics>());

        await consumer.HandleMessageAsync(message, CancellationToken.None);

        eventRepositoryMock.Verify(
            x => x.AddOutboxAsync(It.Is<OutboxMessage>(o => o.Type == nameof(BookingNotApproved)), It.IsAny<CancellationToken>()),
            Times.Once);
        processedRepositoryMock.Verify(
            x => x.AddAsync(message.MessageId, nameof(BookingNotApproved), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "BookingRequested")]
    public async Task BookingRequestedConsumer_Should_Reserve_Seats_And_Send_BookingConfirmed()
    {
        var services = new ServiceCollection();
        var processedRepositoryMock = new Mock<IProcessedMessageRepository>();
        var eventRepositoryMock = new Mock<IEventRepository>();
        FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));


        var message = new BookingRequested(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2, timeProvider.GetUtcNow().UtcDateTime);
        var futureEvent = Event.Create("Future event", timeProvider.GetUtcNow().UtcDateTime.AddDays(1), timeProvider.GetUtcNow().UtcDateTime.AddDays(1).AddHours(2), 10);

        processedRepositoryMock
            .Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        eventRepositoryMock
            .Setup(x => x.GetByIdAsync(message.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(futureEvent);

        services.AddSingleton(processedRepositoryMock.Object);
        services.AddSingleton(eventRepositoryMock.Object);
        await using var provider = services.BuildServiceProvider();
        var processor = CreateProcessor(provider, timeProvider);
        var retryPolicy = CreateDbRetryPolicy();

        using var consumer = new BookingRequestedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "events-tests" }),
            Mock.Of<ILogger<BookingRequestedConsumer>>(),
            processor,
            retryPolicy,
            timeProvider, Mock.Of<KafkaMetrics>());

        await consumer.HandleMessageAsync(message, CancellationToken.None);

        // Доменный метод TryReserveSeats должен был сработать
        futureEvent.AvailableSeats.Should().Be(8);
        eventRepositoryMock.Verify(
            x => x.SaveEventAndOutboxAsync(futureEvent, It.Is<OutboxMessage>(o => o.Type == nameof(BookingConfirmed)), It.IsAny<CancellationToken>()),
            Times.Once);
        processedRepositoryMock.Verify(
            x => x.AddAsync(message.MessageId, nameof(BookingConfirmed), It.IsAny<CancellationToken>()),
            Times.Once);
    }

}

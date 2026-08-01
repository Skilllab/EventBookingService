using System;
using System.Threading;
using System.Threading.Tasks;

using Confluent.Kafka;

using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Infrastructure.Entities;
using EventForge.Booking.Infrastructure.Services;
using EventForge.Contract.Brokers;
using EventForge.Contract.Enums;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Moq;

using Xunit;

namespace EventForge.Booking.UnitTests;

public class BackgroundAndKafkaTests
{

    [Fact]
    public async Task KafkaBookingConfirmedPublisher_Should_Publish_Raw_Message_With_Expected_Topic_And_Key()
    {
        // Arrange
        var producerMock = new Mock<IProducer<string, string>>();
        var loggerMock = new Mock<ILogger<KafkaBookingPublisher>>();
        using var publisher = new KafkaBookingPublisher(producerMock.Object, loggerMock.Object, Mock.Of<KafkaMetrics>());

        const string topic = TopicNames.BookingConfirmed;
        const string key = "event-key";
        const string payload = """{"eventId":"11111111-1111-1111-1111-111111111111"}""";

        producerMock
            .Setup(x => x.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeliveryResult<string, string>) null!);

        // Act
        await publisher.PublishRawAsync(topic, key, payload, CancellationToken.None);

        // Assert
        producerMock.Verify(x => x.ProduceAsync(
            topic,
            It.Is<Message<string, string>>(m => m.Key == key && m.Value == payload),
            It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    [Trait("Category", "BookingConfirmedConsumer")]
    public async Task BookingConfirmedConsumer_Should_Confirm_Booking_When_Pending()
    {
        var services = new ServiceCollection();
        var processedRepoMock = new Mock<IProcessedMessageRepository>();
        var bookingRepoMock = new Mock<IBookingRepository>();
        FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));


        var message = new BookingConfirmed(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, _timeProvider.GetUtcNow().UtcDateTime);
        var booking = BookingModel.Create(message.EventId, message.UserId, _timeProvider.GetUtcNow().UtcDateTime);

        processedRepoMock.Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        bookingRepoMock.Setup(x => x.GetByIdAsync(message.BookingId, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        bookingRepoMock.Setup(x => x.ConfirmBookingAsync(message.BookingId, message.ConfirmedAt, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        services.AddSingleton(processedRepoMock.Object);
        services.AddSingleton(bookingRepoMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var consumer = new BookingConfirmedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "booking-tests" }),
            Mock.Of<ILogger<BookingConfirmedConsumer>>(), Mock.Of<KafkaMetrics>());

        await CallHandleMessageAsync<BookingConfirmed, BookingConfirmedConsumer>(consumer, message, CancellationToken.None);

        bookingRepoMock.Verify(x => x.ConfirmBookingAsync(message.BookingId, message.ConfirmedAt, It.IsAny<CancellationToken>()), Times.Once);
        processedRepoMock.Verify(x => x.AddAsync(message.MessageId, nameof(BookingConfirmed), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "BookingConfirmedConsumer")]
    public async Task BookingConfirmedConsumer_Should_Skip_When_Already_Processed()
    {
        var services = new ServiceCollection();
        var processedRepoMock = new Mock<IProcessedMessageRepository>();
        var bookingRepoMock = new Mock<IBookingRepository>();
        FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));


        var message = new BookingConfirmed(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, _timeProvider.GetUtcNow().UtcDateTime);

        processedRepoMock.Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        services.AddSingleton(processedRepoMock.Object);
        services.AddSingleton(bookingRepoMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var consumer = new BookingConfirmedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "booking-tests" }),
            Mock.Of<ILogger<BookingConfirmedConsumer>>(), Mock.Of<KafkaMetrics>());

        await CallHandleMessageAsync<BookingConfirmed, BookingConfirmedConsumer>(consumer, message, CancellationToken.None);

        bookingRepoMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "BookingConfirmedConsumer")]
    public async Task BookingConfirmedConsumer_Should_MarkProcessed_When_Booking_Not_Found()
    {
        var services = new ServiceCollection();
        var processedRepoMock = new Mock<IProcessedMessageRepository>();
        var bookingRepoMock = new Mock<IBookingRepository>();
        FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));


        var message = new BookingConfirmed(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, _timeProvider.GetUtcNow().UtcDateTime);

        processedRepoMock.Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        bookingRepoMock.Setup(x => x.GetByIdAsync(message.BookingId, It.IsAny<CancellationToken>())).ReturnsAsync((BookingModel?) null);

        services.AddSingleton(processedRepoMock.Object);
        services.AddSingleton(bookingRepoMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var consumer = new BookingConfirmedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "booking-tests" }),
            Mock.Of<ILogger<BookingConfirmedConsumer>>(), Mock.Of<KafkaMetrics>());

        await CallHandleMessageAsync<BookingConfirmed, BookingConfirmedConsumer>(consumer, message, CancellationToken.None);

        processedRepoMock.Verify(x => x.AddAsync(message.MessageId, nameof(BookingConfirmed), It.IsAny<CancellationToken>()), Times.Once);
        bookingRepoMock.Verify(x => x.ConfirmBookingAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "BookingConfirmedConsumer")]
    public async Task BookingConfirmedConsumer_Should_Skip_When_Booking_Already_Confirmed()
    {
        var services = new ServiceCollection();
        var processedRepoMock = new Mock<IProcessedMessageRepository>();
        var bookingRepoMock = new Mock<IBookingRepository>();
        FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));


        var message = new BookingConfirmed(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, _timeProvider.GetUtcNow().UtcDateTime);
        var booking = BookingModel.Create(message.EventId, message.UserId, _timeProvider.GetUtcNow().UtcDateTime);
        booking.Confirm(_timeProvider.GetUtcNow().UtcDateTime); // уже подтверждён

        processedRepoMock.Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        bookingRepoMock.Setup(x => x.GetByIdAsync(message.BookingId, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        services.AddSingleton(processedRepoMock.Object);
        services.AddSingleton(bookingRepoMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var consumer = new BookingConfirmedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "booking-tests" }),
            Mock.Of<ILogger<BookingConfirmedConsumer>>(), Mock.Of<KafkaMetrics>());

        await CallHandleMessageAsync<BookingConfirmed, BookingConfirmedConsumer>(consumer, message, CancellationToken.None);

        bookingRepoMock.Verify(x => x.ConfirmBookingAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        processedRepoMock.Verify(x => x.AddAsync(message.MessageId, nameof(BookingConfirmed), It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    [Trait("Category", "BookingRejectedConsumer")]
    public async Task BookingRejectedConsumer_Should_Reject_Booking_When_Pending()
    {
        var services = new ServiceCollection();
        var processedRepoMock = new Mock<IProcessedMessageRepository>();
        var bookingRepoMock = new Mock<IBookingRepository>();
        FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));


        var message = new BookingRejected(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _timeProvider.GetUtcNow().UtcDateTime, "no seats available");
        var booking = BookingModel.Create(message.EventId, message.UserId, _timeProvider.GetUtcNow().UtcDateTime.AddHours(-1));

        processedRepoMock.Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        bookingRepoMock.Setup(x => x.GetByIdAsync(message.BookingId, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        bookingRepoMock.Setup(x => x.RejectBookingAsync(message.BookingId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        services.AddSingleton(processedRepoMock.Object);
        services.AddSingleton(bookingRepoMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var consumer = new BookingRejectedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "booking-tests" }),
            Mock.Of<ILogger<BookingRejectedConsumer>>(),
            Mock.Of<KafkaMetrics>());

        await CallHandleMessageAsync<BookingRejected, BookingRejectedConsumer>(consumer, message, CancellationToken.None);

        bookingRepoMock.Verify(x => x.RejectBookingAsync(message.BookingId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        processedRepoMock.Verify(x => x.AddAsync(message.MessageId, nameof(BookingRejected), It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    [Trait("Category", "BookingNotApprovedConsumer")]
    public async Task BookingNotApprovedConsumer_Should_Reject_Booking_When_Pending()
    {
        var services = new ServiceCollection();
        var processedRepoMock = new Mock<IProcessedMessageRepository>();
        var bookingRepoMock = new Mock<IBookingRepository>();
        FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));


        var message = new BookingNotApproved(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), _timeProvider.GetUtcNow().UtcDateTime, BookingNotApprovedReason.NoSeats);
        var booking = BookingModel.Create(message.EventId, message.UserId, _timeProvider.GetUtcNow().UtcDateTime.AddHours(-1));

        processedRepoMock.Setup(x => x.ExistsAsync(message.MessageId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        bookingRepoMock.Setup(x => x.GetByIdAsync(message.BookingId, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        bookingRepoMock.Setup(x => x.RejectBookingAsync(message.BookingId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        services.AddSingleton(processedRepoMock.Object);
        services.AddSingleton(bookingRepoMock.Object);
        await using var provider = services.BuildServiceProvider();
        using var consumer = new BookingNotApprovedConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092", ConsumerGroup = "booking-tests" }),
            Mock.Of<ILogger<BookingNotApprovedConsumer>>(),
            Mock.Of<KafkaMetrics>());

        await CallHandleMessageAsync<BookingNotApproved, BookingNotApprovedConsumer>(consumer, message, CancellationToken.None);

        bookingRepoMock.Verify(x => x.RejectBookingAsync(message.BookingId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        processedRepoMock.Verify(x => x.AddAsync(message.MessageId, nameof(BookingNotApproved), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // Вспомогательный метод для вызова private HandleMessageAsync через рефлексию
    // ========================================================================
    private static async Task CallHandleMessageAsync<TMessage, TConsumer>(
        TConsumer consumer, TMessage? message, CancellationToken ct)
    {
        var method = typeof(TConsumer).GetMethod(
            "HandleMessageAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task) (method!.Invoke(consumer, [message, ct])!);
    }





}

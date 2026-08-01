using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using EventForge.Booking.Application.Common;
using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Application.Services;
using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Domain.Exceptions;
using EventForge.Contract.Brokers;
using EventForge.Shared.Enums;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Moq;

using Xunit;

namespace EventForge.Booking.UnitTests;

public class BookingServiceTests
{

    [Fact]
    [Trait("Category", "GetBooking")]
    public async Task GetBookingByIdAsync_Should_Return_DTO_When_Booking_Found()
    {
        // Arrange
        var repositoryMock = new Mock<IBookingRepository>();
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            TimeProvider.System);
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var booking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), fakeTimeProvider.GetUtcNow().UtcDateTime);

        repositoryMock
            .Setup(x => x.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        var result = await service.GetBookingByIdAsync(booking.Id, CancellationToken.None);

        // Assert
        result.ID.Should().Be(booking.Id);
        result.EventID.Should().Be(booking.EventId);
        result.Status.Should().Be(nameof(BookingStatus.Pending));
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_Should_Throw_NotFound_When_Booking_Does_Not_Exist()
    {
        // Arrange
        var repositoryMock = new Mock<IBookingRepository>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            fakeTimeProvider);
        var bookingId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingModel?) null);

        // Act
        Func<Task> act = () => service.CancelBooking(bookingId, Guid.NewGuid(), RoleType.User, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_Should_Throw_When_Already_Cancelled()
    {
        // Arrange
        var repositoryMock = new Mock<IBookingRepository>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            fakeTimeProvider);
        var booking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), fakeTimeProvider.GetUtcNow().UtcDateTime);
        booking.Cancel(fakeTimeProvider.GetUtcNow().UtcDateTime);

        repositoryMock
            .Setup(x => x.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        Func<Task> act = () => service.CancelBooking(booking.Id, booking.UserId, RoleType.User, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_Should_Throw_When_Already_Rejected()
    {
        // Arrange
        var repositoryMock = new Mock<IBookingRepository>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            fakeTimeProvider);
        var booking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), fakeTimeProvider.GetUtcNow().UtcDateTime);
        booking.Reject(fakeTimeProvider.GetUtcNow().UtcDateTime);

        repositoryMock
            .Setup(x => x.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        Func<Task> act = () => service.CancelBooking(booking.Id, booking.UserId, RoleType.User, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_Should_Allow_Admin_To_Cancel_Other_Users_Booking()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var repositoryMock = new Mock<IBookingRepository>();
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            fakeTimeProvider);
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var booking = BookingModel.Create(Guid.NewGuid(), ownerId, fakeTimeProvider.GetUtcNow().UtcDateTime);

        repositoryMock
            .Setup(x => x.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        repositoryMock
            .Setup(x => x.CancelAndAddOutboxAsync(booking.Id, ownerId, It.IsAny<DateTime>(), It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await service.CancelBooking(booking.Id, adminId, RoleType.Admin, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        repositoryMock.Verify(
            x => x.CancelAndAddOutboxAsync(booking.Id, ownerId, It.IsAny<DateTime>(), It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_Should_Throw_When_Repository_Returns_False()
    {
        // Arrange
        var repositoryMock = new Mock<IBookingRepository>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            fakeTimeProvider);
        var booking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), fakeTimeProvider.GetUtcNow().UtcDateTime);

        repositoryMock
            .Setup(x => x.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        repositoryMock
            .Setup(x => x.CancelAndAddOutboxAsync(booking.Id, booking.UserId, It.IsAny<DateTime>(), It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = () => service.CancelBooking(booking.Id, booking.UserId, RoleType.User, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    [Trait("Category", "GetAllBooking")]
    public async Task GetAllBooking_Should_Return_User_Bookings_When_Admin()
    {
        // Arrange
        var repositoryMock = new Mock<IBookingRepository>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            fakeTimeProvider);
        var userId = Guid.NewGuid();
        var userBooking = BookingModel.Create(Guid.NewGuid(), userId, fakeTimeProvider.GetUtcNow().UtcDateTime);
        var otherBooking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), fakeTimeProvider.GetUtcNow().UtcDateTime);

        repositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([userBooking, otherBooking]);

        // Act
        var result = await service.GetAllBooking(userId, RoleType.Admin, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].ID.Should().Be(userBooking.Id);
    }

    [Fact]
    [Trait("Category", "GetAllBooking")]
    public async Task GetAllBooking_Should_Throw_When_Not_Admin()
    {
        // Arrange
        var repositoryMock = new Mock<IBookingRepository>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            fakeTimeProvider);

        // Act
        Func<Task> act = () => service.GetAllBooking(Guid.NewGuid(), RoleType.User, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InsufficientPermissionsException>();
    }



   

    [Fact]
    [Trait("Category", "CreateBooking")]
    public async Task CreateBookingAsync_Should_Throw_When_Limit_Exceeded()
    {
        // Arrange
        var repositoryMock = new Mock<IBookingRepository>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 2 }),
            Mock.Of<ILogger<BookingService>>(),
            fakeTimeProvider);
        var userId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.GetUserActiveBookingsCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        Func<Task> act = () => service.CreateBookingAsync(Guid.NewGuid(), userId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BookingLimitExceededException>();
    }

    [Fact]
    [Trait("Category", "GetBooking")]
    public async Task GetBookingByIdAsync_Should_Throw_When_Booking_Not_Found()
    {
        // Arrange
        var repositoryMock = new Mock<IBookingRepository>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            fakeTimeProvider);
        var bookingId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingModel?)null);

        // Act
        Func<Task> act = () => service.GetBookingByIdAsync(bookingId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_Should_Throw_When_User_Has_No_Permission()
    {
        // Arrange
        var repositoryMock = new Mock<IBookingRepository>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            fakeTimeProvider);
        var booking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), fakeTimeProvider.GetUtcNow().UtcDateTime);

        repositoryMock
            .Setup(x => x.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        // Act
        Func<Task> act = () => service.CancelBooking(booking.Id, Guid.NewGuid(), RoleType.User, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InsufficientPermissionsException>();
    }

    [Fact]
    [Trait("Category", "CancelBooking")]
    public async Task CancelBooking_Should_Save_Cancelled_Outbox_For_Owner()
    {
        // Arrange
        var repositoryMock = new Mock<IBookingRepository>();
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 7, 4, 15, 0, 0, TimeSpan.Zero));
        var service = new BookingService(
            repositoryMock.Object,
            Options.Create(new BookingOptions { MaxBookingCount = 3 }),
            Mock.Of<ILogger<BookingService>>(),
            fakeTimeProvider);
        var ownerId = Guid.NewGuid();
        var booking = BookingModel.Create(Guid.NewGuid(), ownerId, fakeTimeProvider.GetUtcNow().UtcDateTime);

        repositoryMock
            .Setup(x => x.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);

        OutboxMessage? savedOutbox = null;
        repositoryMock
            .Setup(x => x.CancelAndAddOutboxAsync(booking.Id, ownerId, It.IsAny<DateTime>(), It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, DateTime, OutboxMessage, CancellationToken>((_, _, _, outbox, _) => savedOutbox = outbox)
            .ReturnsAsync(true);

        // Act
        var result = await service.CancelBooking(booking.Id, ownerId, RoleType.User, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        savedOutbox.Should().NotBeNull();
        savedOutbox!.Topic.Should().Be(TopicNames.BookingCancelled);
        var payload = JsonSerializer.Deserialize<BookingCancelled>(savedOutbox.Payload);
        payload.Should().NotBeNull();
        payload!.BookingId.Should().Be(booking.Id);
    }
}

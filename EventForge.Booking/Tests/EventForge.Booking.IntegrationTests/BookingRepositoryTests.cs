using System;
using System.Threading;
using System.Threading.Tasks;

using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Infrastructure.Mapping;
using EventForge.Booking.Infrastructure.Repositories;
using EventForge.Contract.Brokers;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace EventForge.Booking.IntegrationTests;

public class BookingRepositoryTests : BaseRepositoryTest
{
    // Фиксированное время для детерминированных тестов
    private static readonly DateTimeOffset _now = new(2025, 7, 4, 15, 0, 0, TimeSpan.Zero);

    private BookingRepository CreateRepository() => new(Factory);

    /// <summary>Прямой insert бронирования в БД (минуя репозиторий).</summary>
    private async Task<BookingModel> SeedBookingAsync(BookingModel booking)
    {
        await using var context = await CreateContext();
        await context.AddAsync(booking.ToEntity());
        await context.SaveChangesAsync();
        return booking;
    }

    [Fact]
    [Trait("Category", "GetById")]
    public async Task GetByIdAsync_Should_Return_Booking_When_Exists()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var booking = await SeedBookingAsync(
            BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), _now.UtcDateTime));

        var result = await repository.GetByIdAsync(booking.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(booking.Id);
        result.Status.Should().Be(BookingStatus.Pending);
    }

    [Fact]
    [Trait("Category", "GetById")]
    public async Task GetByIdAsync_Should_Return_Null_When_Not_Exists()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }


    [Fact]
    [Trait("Category", "GetAll")]
    public async Task GetAllAsync_Should_Return_All_Bookings()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        await SeedBookingAsync(BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), _now.UtcDateTime));
        await SeedBookingAsync(BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), _now.UtcDateTime));

        var result = await repository.GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "GetAll")]
    public async Task GetAllAsync_Should_Return_Empty_When_No_Bookings()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();

        var result = await repository.GetAllAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }


    [Fact]
    [Trait("Category", "GetUserActiveBookingsCount")]
    public async Task GetUserActiveBookingsCountAsync_Should_Count_Only_Pending_And_Confirmed()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var userId = Guid.NewGuid();

        var pending = BookingModel.Create(Guid.NewGuid(), userId, _now.UtcDateTime);
        var confirmed = BookingModel.Create(Guid.NewGuid(), userId, _now.UtcDateTime);
        confirmed.Confirm(_now.UtcDateTime);
        var cancelled = BookingModel.Create(Guid.NewGuid(), userId, _now.UtcDateTime);
        cancelled.Cancel(_now.UtcDateTime);
        var rejected = BookingModel.Create(Guid.NewGuid(), userId, _now.UtcDateTime);
        rejected.Reject(_now.UtcDateTime);

        await SeedBookingAsync(pending);
        await SeedBookingAsync(confirmed);
        await SeedBookingAsync(cancelled);
        await SeedBookingAsync(rejected);

        var result = await repository.GetUserActiveBookingsCountAsync(userId, CancellationToken.None);

        result.Should().Be(2); // pending + confirmed
    }

    [Fact]
    [Trait("Category", "GetUserActiveBookingsCount")]
    public async Task GetUserActiveBookingsCountAsync_Should_Return_Zero_For_Unknown_User()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();

        var result = await repository.GetUserActiveBookingsCountAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().Be(0);
    }


    [Fact]
    [Trait("Category", "CreateAndAddOutbox")]
    public async Task CreateAndAddOutboxAsync_Should_Save_Booking_And_Outbox_Together()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var booking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), _now.UtcDateTime);
        var outbox = OutboxMessage.Create(
            nameof(BookingRequested), TopicNames.BookingRequested,
            booking.EventId.ToString(), "{}", _now.UtcDateTime, null);

        var result = await repository.CreateAndAddOutboxAsync(booking, outbox, CancellationToken.None);

        result.Should().BeTrue();

        var saved = await repository.GetByIdAsync(booking.Id, CancellationToken.None);
        saved.Should().NotBeNull();
        saved!.Id.Should().Be(booking.Id);

        await using var context = await CreateContext();
        var outboxCount = await context.OutboxMessages.CountAsync(TestContext.Current.CancellationToken);
        outboxCount.Should().Be(1);
    }


    [Fact]
    [Trait("Category", "CancelAndAddOutbox")]
    public async Task CancelAndAddOutboxAsync_Should_Update_Status_And_Create_Outbox()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var userId = Guid.NewGuid();
        var booking = await SeedBookingAsync(
            BookingModel.Create(Guid.NewGuid(), userId, _now.UtcDateTime));
        var processedAt = _now.UtcDateTime;
        var outbox = OutboxMessage.Create(
            nameof(BookingCancelled), TopicNames.BookingCancelled,
            booking.EventId.ToString(), "{}", processedAt, null);

        var updated = await repository.CancelAndAddOutboxAsync(
            booking.Id, userId, processedAt, outbox, CancellationToken.None);

        updated.Should().BeTrue();
        var result = await repository.GetByIdAsync(booking.Id, CancellationToken.None);
        result.Should().NotBeNull();
        result!.Status.Should().Be(BookingStatus.Cancelled);
        result.ProcessedAt.Should().Be(processedAt);

        await using var context = await CreateContext();
        var outboxCount = await context.OutboxMessages.CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        outboxCount.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "CancelAndAddOutbox")]
    public async Task CancelAndAddOutboxAsync_Should_Return_False_When_Booking_Not_Found()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var outbox = OutboxMessage.Create(
            "Test", "topic", "key", "{}", _now.UtcDateTime, null);

        var result = await repository.CancelAndAddOutboxAsync(
            Guid.NewGuid(), Guid.NewGuid(), _now.UtcDateTime, outbox, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "CancelAndAddOutbox")]
    public async Task CancelAndAddOutboxAsync_Should_Return_False_When_User_Does_Not_Own_Booking()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var booking = await SeedBookingAsync(
            BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), _now.UtcDateTime));
        var outbox = OutboxMessage.Create(
            "Test", "topic", "key", "{}", _now.UtcDateTime, null);

        var result = await repository.CancelAndAddOutboxAsync(
            booking.Id, Guid.NewGuid(), _now.UtcDateTime, outbox, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "CancelAndAddOutbox")]
    public async Task CancelAndAddOutboxAsync_Should_Return_False_When_Already_Cancelled()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var userId = Guid.NewGuid();
        var booking = BookingModel.Create(Guid.NewGuid(), userId, _now.UtcDateTime);
        booking.Cancel(_now.UtcDateTime);
        await SeedBookingAsync(booking);
        var outbox = OutboxMessage.Create(
            "Test", "topic", "key", "{}", _now.UtcDateTime, null);

        var result = await repository.CancelAndAddOutboxAsync(
            booking.Id, userId, _now.UtcDateTime, outbox, CancellationToken.None);

        result.Should().BeFalse();
    }


    [Fact]
    [Trait("Category", "ConfirmBooking")]
    public async Task ConfirmBookingAsync_Should_Update_Status_To_Confirmed()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var booking = await SeedBookingAsync(
            BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), _now.UtcDateTime));
        var processedAt = _now.UtcDateTime;

        var updated = await repository.ConfirmBookingAsync(
            booking.Id, processedAt, CancellationToken.None);

        updated.Should().BeTrue();
        var result = await repository.GetByIdAsync(booking.Id, CancellationToken.None);
        result.Should().NotBeNull();
        result!.Status.Should().Be(BookingStatus.Confirmed);
        result.ProcessedAt.Should().Be(processedAt);
    }

    [Fact]
    [Trait("Category", "ConfirmBooking")]
    public async Task ConfirmBookingAsync_Should_Return_False_When_Booking_Not_Found()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();

        var result = await repository.ConfirmBookingAsync(
            Guid.NewGuid(), _now.UtcDateTime, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "ConfirmBooking")]
    public async Task ConfirmBookingAsync_Should_Return_False_When_Already_Confirmed()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var booking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), _now.UtcDateTime);
        booking.Confirm(_now.UtcDateTime);
        await SeedBookingAsync(booking);

        var result = await repository.ConfirmBookingAsync(
            booking.Id, _now.UtcDateTime, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "ConfirmBooking")]
    public async Task ConfirmBookingAsync_Should_Return_False_When_Cancelled()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var booking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), _now.UtcDateTime);
        booking.Cancel(_now.UtcDateTime);
        await SeedBookingAsync(booking);

        var result = await repository.ConfirmBookingAsync(
            booking.Id, _now.UtcDateTime, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "RejectBooking")]
    public async Task RejectBookingAsync_Should_Update_Status_To_Rejected()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var booking = await SeedBookingAsync(
            BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), _now.UtcDateTime));
        var processedAt = _now.UtcDateTime;

        var updated = await repository.RejectBookingAsync(
            booking.Id, processedAt, CancellationToken.None);

        updated.Should().BeTrue();
        var result = await repository.GetByIdAsync(booking.Id, CancellationToken.None);
        result.Should().NotBeNull();
        result!.Status.Should().Be(BookingStatus.Rejected);
        result.ProcessedAt.Should().Be(processedAt);
    }

    [Fact]
    [Trait("Category", "RejectBooking")]
    public async Task RejectBookingAsync_Should_Return_False_When_Booking_Not_Found()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();

        var result = await repository.RejectBookingAsync(
            Guid.NewGuid(), _now.UtcDateTime, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "RejectBooking")]
    public async Task RejectBookingAsync_Should_Return_False_When_Already_Rejected()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var booking = BookingModel.Create(Guid.NewGuid(), Guid.NewGuid(), _now.UtcDateTime);
        booking.Reject(_now.UtcDateTime);
        await SeedBookingAsync(booking);

        var result = await repository.RejectBookingAsync(
            booking.Id, _now.UtcDateTime, CancellationToken.None);

        result.Should().BeFalse();
    }
}

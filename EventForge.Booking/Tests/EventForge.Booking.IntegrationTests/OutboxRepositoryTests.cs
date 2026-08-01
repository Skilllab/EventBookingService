using System;
using System.Threading;
using System.Threading.Tasks;

using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Infrastructure.Mapping;
using EventForge.Booking.Infrastructure.Repositories;
using EventForge.Contract.Brokers;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Xunit;

namespace EventForge.Booking.IntegrationTests;

public class OutboxRepositoryTests : BaseRepositoryTest
{
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));
    private OutboxRepository CreateRepository() => new(Factory, _timeProvider);

    [Fact]
    public async Task GetPendingAsync_Should_Return_Only_Unprocessed_Messages_Ordered_By_CreatedAt()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repository = CreateRepository();

        var newestPending = OutboxMessage.Create(
            "Бронь подтверждена",
            TopicNames.BookingConfirmed,
            "event-key-2",
            "payload-2",
            _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-5),
            null);

        var oldestPending = OutboxMessage.Create(
            "Бронь подтверждена",
            TopicNames.BookingConfirmed,
            "event-key-1",
            "payload-1",
            _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-10),
            null);

        var processed = OutboxMessage.Restore(
            Guid.NewGuid(),
            "Бронь подтверждена",
            TopicNames.BookingConfirmed,
            "event-key-3",
            "payload-3",
            _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-1),
            _timeProvider.GetUtcNow().UtcDateTime,
            null,
            null,
            null);

        await using var context = await CreateContext();
        await context.OutboxMessages.AddRangeAsync(
            oldestPending.ToEntity(),
            newestPending.ToEntity(),
            processed.ToEntity());
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await repository.GetPendingAsync(10, CancellationToken.None);
        
        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder(oldestPending.Id, newestPending.Id);
    }

    [Fact]
    public async Task GetPendingAsync_Should_Respect_Batch_Size()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var first = OutboxMessage.Create("Бронь подтверждена", TopicNames.BookingConfirmed, "key-1", "payload-1", _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-3), null);
        var second = OutboxMessage.Create("Бронь подтверждена", TopicNames.BookingConfirmed, "key-2", "payload-2", _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-2), null);
        var third = OutboxMessage.Create("Бронь подтверждена", TopicNames.BookingConfirmed, "key-3", "payload-3", _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-1), null);

        await using var context = await CreateContext();
        await context.OutboxMessages.AddRangeAsync(first.ToEntity(), second.ToEntity(), third.ToEntity());
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act  
        var result = await repository.GetPendingAsync(2, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().ContainInOrder(first.Id, second.Id);
    }

    [Fact]
    public async Task MarkProcessedAsync_Should_Set_ProcessedAt_And_Clear_Error()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var message = OutboxMessage.Restore(
            Guid.NewGuid(),
            "Бронь подтверждена",
            TopicNames.BookingConfirmed,
            "event-key",
            "payload",
            _timeProvider.GetUtcNow().UtcDateTime,
            null,
            "старая ошибка", 
            null,
            null);

        await using var arrangeContext = await CreateContext();
        await arrangeContext.OutboxMessages.AddAsync(message.ToEntity(), TestContext.Current.CancellationToken);
        await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await repository.MarkProcessedAsync(message.Id, CancellationToken.None);

        // Act
        await using var assertContext = await CreateContext();
        var entity = await assertContext.OutboxMessages.FindAsync([message.Id], TestContext.Current.CancellationToken);

        // Assert
        entity.Should().NotBeNull();
        entity!.ProcessedAt.Should().NotBeNull();
        entity.Error.Should().BeNull();
    }

    [Fact]
    public async Task MarkFailedAsync_Should_Save_Error_Message()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var message = OutboxMessage.Create(
            "BookingConfirmed",
            TopicNames.BookingConfirmed,
            "event-key",
            "payload",
            _timeProvider.GetUtcNow().UtcDateTime,
            null);

        await using var arrangeContext = await CreateContext();
        await arrangeContext.OutboxMessages.AddAsync(message.ToEntity(), TestContext.Current.CancellationToken);
        await arrangeContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await repository.MarkFailedAsync(message.Id, "публикация провалена", CancellationToken.None);

        await using var assertContext = await CreateContext();
        var entity = await assertContext.OutboxMessages.FindAsync([message.Id], TestContext.Current.CancellationToken);

        // Assert
        entity.Should().NotBeNull();
        entity!.Error.Should().Be("публикация провалена");
        entity.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task MarkProcessedAsync_Should_Do_Nothing_When_Message_Not_Found()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repository = CreateRepository();

        // Act
        var act = async () => await repository.MarkProcessedAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MarkFailedAsync_Should_Do_Nothing_When_Message_Not_Found()
    {
        // Arrange
        await ResetDatabaseAsync();
        var repository = CreateRepository();

        // Act
        var act = async () => await repository.MarkFailedAsync(Guid.NewGuid(), "публикация провалена", CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }
}

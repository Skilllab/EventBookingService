using System;
using System.Threading;
using System.Threading.Tasks;

using EventForge.Events.Domain.Entities;
using EventForge.Events.Infrastructure.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Xunit;

namespace EventForge.Events.IntegrationTests
{
    public class OutboxRepositoryTests : BaseRepositoryTest
    {
        private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));
        private OutboxRepository CreateRepository() => new(Factory, _timeProvider);

        [Fact]
        public async Task GetPendingAsync_Should_Return_Only_Unprocessed_Messages_Ordered_By_CreatedAt()
        {
            await ResetDatabaseAsync();
            var repository = CreateRepository();
            var older = OutboxMessage.Create("Событие один", "topic-a", "key1", "{}", _timeProvider.GetUtcNow().UtcDateTime, null);
            var newer = OutboxMessage.Create("Событие два", "topic-b", "key2", "{}", _timeProvider.GetUtcNow().UtcDateTime.AddSeconds(1), null);

            var eventRepo = new EventRepository(Factory);
            await eventRepo.AddOutboxAsync(older, CancellationToken.None);
            await eventRepo.AddOutboxAsync(newer, CancellationToken.None);

            var pending = await repository.GetPendingAsync(10, CancellationToken.None);

            pending.Should().HaveCount(2);
            pending[0].Id.Should().Be(older.Id);
            pending[1].Id.Should().Be(newer.Id);
        }

        [Fact]
        public async Task MarkProcessedAsync_Should_Set_ProcessedAt_And_Clear_Error()
        {
            await ResetDatabaseAsync();
            var repository = CreateRepository();
            var message = OutboxMessage.Create("Событие один", "topic", "key", "{}", _timeProvider.GetUtcNow().UtcDateTime, null);

            var eventRepo = new EventRepository(Factory);
            await eventRepo.AddOutboxAsync(message, CancellationToken.None);

            await repository.MarkProcessedAsync(message.Id, CancellationToken.None);

            var pending = await repository.GetPendingAsync(10, CancellationToken.None);
            pending.Should().BeEmpty();
        }

        [Fact]
        public async Task MarkFailedAsync_Should_Set_Error_On_Message()
        {
            await ResetDatabaseAsync();
            var repository = CreateRepository();
            var message = OutboxMessage.Create("Событие один", "topic", "key", "{}", _timeProvider.GetUtcNow().UtcDateTime, null);

            var eventRepo = new EventRepository(Factory);
            await eventRepo.AddOutboxAsync(message, CancellationToken.None);

            await repository.MarkFailedAsync(message.Id, "Publish error", CancellationToken.None);

            var pending = await repository.GetPendingAsync(10, CancellationToken.None);
            pending.Should().HaveCount(1);
            pending[0].Error.Should().NotBeEmpty();
        }
    }
}

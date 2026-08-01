using System;
using System.Threading;
using System.Threading.Tasks;

using EventForge.Events.Infrastructure.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Xunit;

namespace EventForge.Events.IntegrationTests;

public class ProcessedMessageRepositoryTests : BaseRepositoryTest
{
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 7, 13, 12, 0, 0, TimeSpan.Zero));
    private ProcessedMessageRepository CreateRepository() => new(Factory, _timeProvider);

    [Fact]
    public async Task AddAsync_Should_Save_Message_And_ExistsAsync_Should_Return_True()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();
        var messageId = Guid.NewGuid();

        await repository.AddAsync(messageId, "BookingConfirmed", CancellationToken.None);
        var exists = await repository.ExistsAsync(messageId, CancellationToken.None);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_False_For_Unknown_Message()
    {
        await ResetDatabaseAsync();
        var repository = CreateRepository();

        var exists = await repository.ExistsAsync(Guid.NewGuid(), CancellationToken.None);

        exists.Should().BeFalse();
    }
}

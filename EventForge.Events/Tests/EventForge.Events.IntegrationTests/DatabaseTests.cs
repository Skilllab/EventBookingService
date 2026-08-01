using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace EventForge.Events.IntegrationTests;

public class DatabaseTests : BaseRepositoryTest
{
    [Fact]
    public async Task Migrations_Should_Apply_Successfully()
    {
        await using var context = await CreateContext();
        await context.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);

        await context.Database.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);
        var eventsCount = await context.Events.CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        var processedCount = await context.ProcessedMessages.CountAsync(cancellationToken: TestContext.Current.CancellationToken);

        eventsCount.Should().Be(0);
        processedCount.Should().Be(0);
    }
}

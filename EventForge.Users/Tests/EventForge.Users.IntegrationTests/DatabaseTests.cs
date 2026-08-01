using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace EventForge.Users.IntegrationTests;

public class DatabaseTests : BaseRepositoryTest
{
    [Fact]
    public async Task Migrations_Should_Apply_Successfully()
    {
        await using var context = await CreateContext();
        await context.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);

        await context.Database.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);
        var count = await context.Users.CountAsync(cancellationToken: TestContext.Current.CancellationToken);

        count.Should().Be(0);
    }
}

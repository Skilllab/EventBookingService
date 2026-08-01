using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace EventForge.Booking.IntegrationTests;

public class DatabaseTests : BaseRepositoryTest
{
    [Fact]
    public async Task Migrations_Should_Apply_Successfully()
    {
        // Arrange
        await using var context = await CreateContext();
        await context.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
        
        // Act
        await context.Database.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);
        var bookingsCount = await context.Bookings.CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        var outboxCount = await context.OutboxMessages.CountAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        bookingsCount.Should().Be(0);
        outboxCount.Should().Be(0);
    }
}

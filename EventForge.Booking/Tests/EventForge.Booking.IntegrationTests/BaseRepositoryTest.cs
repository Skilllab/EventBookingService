using System.Threading.Tasks;

using EventForge.Booking.Infrastructure.Context;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Testcontainers.PostgreSql;

using Xunit;

namespace EventForge.Booking.IntegrationTests;

public abstract class BaseRepositoryTest : IAsyncLifetime
{
    protected readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("eventforge_booking")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected IDbContextFactory<BookingDbContext> Factory = null!;

    public async ValueTask InitializeAsync()
    {
        await DbContainer.StartAsync();

        var services = new ServiceCollection();
        services.AddPooledDbContextFactory<BookingDbContext>(options =>
            options.UseNpgsql(DbContainer.GetConnectionString()));

        var serviceProvider = services.BuildServiceProvider();
        Factory = serviceProvider.GetRequiredService<IDbContextFactory<BookingDbContext>>();

        await using var context = await CreateContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await DbContainer.DisposeAsync();

    protected async Task<BookingDbContext> CreateContext() => await Factory.CreateDbContextAsync();

    protected async Task ResetDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var context = await CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Booking\".\"OutboxMessages\", \"Booking\".\"Bookings\" RESTART IDENTITY CASCADE;");
    }
}

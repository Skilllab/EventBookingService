using System.Threading.Tasks;

using EventForge.Events.Infrastructure.Context;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Testcontainers.PostgreSql;

using Xunit;

namespace EventForge.Events.IntegrationTests;

public abstract class BaseRepositoryTest : IAsyncLifetime
{
    protected readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("eventforge_events")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected IDbContextFactory<EventsDbContext> Factory = null!;

    public async ValueTask InitializeAsync()
    {
        await DbContainer.StartAsync();

        var services = new ServiceCollection();
        services.AddPooledDbContextFactory<EventsDbContext>(options =>
            options.UseNpgsql(DbContainer.GetConnectionString()));

        var serviceProvider = services.BuildServiceProvider();
        Factory = serviceProvider.GetRequiredService<IDbContextFactory<EventsDbContext>>();

        await using var context = await CreateContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await DbContainer.DisposeAsync();

    protected async Task<EventsDbContext> CreateContext() => await Factory.CreateDbContextAsync();

    protected async Task ResetDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var context = await CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Events\".\"ProcessedMessages\", \"Events\".\"Events\" RESTART IDENTITY CASCADE;");
    }
}

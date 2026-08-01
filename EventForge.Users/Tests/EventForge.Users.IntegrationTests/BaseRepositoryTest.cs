using System.Threading.Tasks;

using EventForge.Users.Infrastructure.Context;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using Testcontainers.PostgreSql;

using Xunit;

namespace EventForge.Users.IntegrationTests;

public abstract class BaseRepositoryTest : IAsyncLifetime
{
    protected readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("eventforge_users")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    protected IDbContextFactory<UsersDbContext> Factory = null!;

    public async ValueTask InitializeAsync()
    {
        await DbContainer.StartAsync();

        var services = new ServiceCollection();
        services.AddPooledDbContextFactory<UsersDbContext>(options =>
            options.UseNpgsql(DbContainer.GetConnectionString()));

        var serviceProvider = services.BuildServiceProvider();
        Factory = serviceProvider.GetRequiredService<IDbContextFactory<UsersDbContext>>();

        await using var context = await CreateContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await DbContainer.DisposeAsync();

    protected async Task<UsersDbContext> CreateContext()
    {
        return await Factory.CreateDbContextAsync();
    }

    protected async Task ResetDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var context = await CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Users\".\"Users\" RESTART IDENTITY CASCADE;");
    }
}

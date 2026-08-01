using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using EventForge.Users.Infrastructure.Context;
using EventForge.Users.Presentation.DTO;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Testcontainers.PostgreSql;

using Xunit;

namespace EventForge.Users.e2eTests;

public class E2EUsersTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eventforge_users_e2e")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();

#pragma warning disable CA2000
        _factory = new WebApplicationFactory<Program>()
#pragma warning restore CA2000
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTests");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString(),
                        ["JwtSettings:Secret"] = "super-secret-key-with-sufficient-length-12345",
                        ["JwtSettings:Issuer"] = "eventforge-users",
                        ["JwtSettings:Audience"] = "eventforge-clients",
                        ["JwtSettings:Lifetime"] = "2",
                        ["JwtSettings:SchemeName"] = "Bearer"
                    });
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Миграция/создание — один раз при старте класса
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<UsersDbContext>>();
        await using var ctx = await db.CreateDbContextAsync();
        await ctx.Database.EnsureCreatedAsync();  // один раз
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task Register_Should_Return_NoContent_And_Save_User()
    {
        // Arrange
        await ResetDatabaseAsync();

        var request = new CreateUserRequest
        {
            Login = "new-user",
            Password = "password123",
            Role = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/register", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<UsersDbContext>>();
        await using var context = await dbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        var user = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Login == "new-user", cancellationToken: TestContext.Current.CancellationToken);

        user.Should().NotBeNull();
        user!.Login.Should().Be("new-user");
        user.PasswordHash.Should().NotBeNullOrWhiteSpace();
        user.Role.Should().Be(EventForge.Shared.Enums.RoleType.User);
    }

    [Fact]
    public async Task Register_Should_Return_BadRequest_When_User_Already_Exists()
    {
        // Arrange
        await ResetDatabaseAsync();

        var request = new CreateUserRequest
        {
            Login = "existing-user",
            Password = "password123",
            Role = "User"
        };

        var first = await _client.PostAsJsonAsync("/auth/register", request, TestContext.Current.CancellationToken);
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        var second = await _client.PostAsJsonAsync("/auth/register", request, TestContext.Current.CancellationToken);

        // Assert
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_Should_Return_Ok_With_Token_When_Credentials_Are_Valid()
    {
        // Arrange
        await ResetDatabaseAsync();

        var registerResponse = await _client.PostAsJsonAsync("/auth/register", new CreateUserRequest
        {
            Login = "known-user",
            Password = "password123",
            Role = "Admin"
        }, TestContext.Current.CancellationToken);

        registerResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginDataRequest
        {
            Login = "known-user",
            Password = "password123"
        }, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: TestContext.Current.CancellationToken);
        payload.Should().NotBeNull();
        payload!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_Should_Return_NotFound_When_Credentials_Are_Invalid()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/auth/login", new LoginDataRequest
        {
            Login = "missing-user",
            Password = "password123"
        }, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Register_Should_Return_BadRequest_When_Model_Is_Invalid()
    {
        // Arrange
        await ResetDatabaseAsync();

        var request = new CreateUserRequest
        {
            Login = "",
            Password = "123",
            Role = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/auth/register", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task ResetDatabaseAsync()
    {
        // Только чистим данные
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<UsersDbContext>>();
        await using var ctx = await db.CreateDbContextAsync();
        await ctx.Users.ExecuteDeleteAsync();
        await ctx.SaveChangesAsync();
    }

    private sealed record LoginResponse(string Token);
}

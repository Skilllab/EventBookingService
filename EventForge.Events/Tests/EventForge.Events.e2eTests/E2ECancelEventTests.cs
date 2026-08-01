using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using EventForge.Events.Infrastructure.Context;
using EventForge.Events.Presentation.DTO;
using EventForge.Shared.Enums;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using Testcontainers.PostgreSql;

using Xunit;

namespace EventForge.Events.e2eTests;

public class E2ECancelEventTests : IAsyncLifetime
{

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eventforge_events_e2e")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _anonymousClient = null!;

    // Константы для JWT (должны совпадать с appsettings)
    private const string JwtSecret = "SuperSecretKeyWithMoreThan32CharactersLength!!";
    private const string JwtIssuer = "EventForge";
    private const string JwtAudience = "EventForgeAPI";

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
                        ["JwtSettings:Secret"] = JwtSecret,
                        ["JwtSettings:Issuer"] = JwtIssuer,
                        ["JwtSettings:Audience"] = JwtAudience,
                        ["JwtSettings:Lifetime"] = "2",
                        ["JwtSettings:SchemeName"] = "Bearer"
                    });
                });
            });

        _anonymousClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Миграция/создание — один раз при старте класса
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<EventsDbContext>>();
        await using var ctx = await db.CreateDbContextAsync();
        await ctx.Database.EnsureCreatedAsync();  // один раз
    }

    public async ValueTask DisposeAsync()
    {
        _anonymousClient.Dispose();
        await _factory.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    /// <summary>
    /// Генерирует JWT-токен с ролью Admin для авторизованных запросов
    /// </summary>
    private static string GenerateAdminToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim("role", nameof(RoleType.Admin)),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Создаёт HttpClient с заголовком Authorization Admin
    /// </summary>
    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateAdminToken());
        return client;
    }

    /// <summary>
    /// Генерирует токен с ролью User
    /// </summary>
    private static string GenerateUserToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim("role", nameof(RoleType.User)),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Создаёт HttpClient с заголовком Authorization User
    /// </summary>
    private HttpClient CreateUserClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateUserToken());
        return client;
    }

    private async Task ResetDatabaseAsync()
    {
        // Только чистим данные
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<EventsDbContext>>();
        await using var ctx = await db.CreateDbContextAsync();
        ctx.Events.RemoveRange(ctx.Events);
        await ctx.SaveChangesAsync();
    }



    [Fact]
    [Trait("Category", "CancelEvent")]
    public async Task CancelEvent_Should_Return_NoContent_When_Admin_Deletes_Event()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var adminClient = CreateAdminClient();
        var startAt = DateTime.UtcNow.AddDays(6);

        var createResponse = await adminClient.PostAsJsonAsync("/events", new CreateEventRequest
        {
            Title = "Будет удалено",
            StartAt = startAt,
            EndAt = startAt.AddHours(1),
            TotalSeats = 5
        }, TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>(cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await adminClient.DeleteAsync($"/events/{created!.Id}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Проверяем, что событие реально удалено
        var getResponse = await _anonymousClient.GetAsync($"/events/{created.Id}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "CancelEvent")]
    public async Task CancelEvent_Should_Return_NotFound_When_Event_Does_Not_Exist()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var client = CreateAdminClient();

        // Act
        var response = await client.DeleteAsync($"/events/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Category", "CancelEvent")]
    public async Task CancelEvent_Should_Return_Unauthorized_When_No_Token()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Act
        var response = await _anonymousClient.DeleteAsync($"/events/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("Category", "CancelEvent")]
    public async Task CancelEvent_Should_Return_Forbidden_When_User_Is_Not_Admin()
    {
        // Arrange
        await ResetDatabaseAsync();
        using var adminClient = CreateAdminClient();
        var startAt = DateTime.UtcNow.AddDays(7);
        var createResponse = await adminClient.PostAsJsonAsync("/events", new CreateEventRequest
        {
            Title = "Защищённое удаление",
            StartAt = startAt,
            EndAt = startAt.AddHours(2),
            TotalSeats = 10
        }, cancellationToken: TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>(cancellationToken: TestContext.Current.CancellationToken);

        using var userClient = CreateUserClient();

        // Act
        var response = await userClient.DeleteAsync($"/events/{created!.Id}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Событие всё ещё существует
        var getResponse = await _anonymousClient.GetAsync($"/events/{created.Id}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

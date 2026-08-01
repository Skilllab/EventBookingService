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

namespace EventForge.Events.e2eTests
{
    public class E2EGetEventTests : IAsyncLifetime
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
        [Trait("Category", "GetEvent")]
        public async Task GetEvent_Should_Return_Ok_When_Event_Exists()
        {
            // Arrange
            await ResetDatabaseAsync();
            using var adminClient = CreateAdminClient();
            var startAt = DateTime.UtcNow.AddDays(3);
            var createRequest = new CreateEventRequest
            {
                Title = "Просто событие",
                StartAt = startAt,
                EndAt = startAt.AddHours(2),
                TotalSeats = 30,
                Description = "Для чтения"
            };

            var createResponse = await adminClient.PostAsJsonAsync("/events", createRequest, cancellationToken: TestContext.Current.CancellationToken);
            var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>(cancellationToken: TestContext.Current.CancellationToken);

            // Act — GET доступен анонимно
            var response = await _anonymousClient.GetAsync($"/events/{created!.Id}", TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<EventResponse>(cancellationToken: TestContext.Current.CancellationToken);
            body.Should().NotBeNull();
            body!.Id.Should().Be(created.Id);
            body.Title.Should().Be("Просто событие");
            body.AvailableSeats.Should().Be(30);
        }

        [Fact]
        [Trait("Category", "GetEvent")]
        public async Task GetEvent_Should_Return_NotFound_When_Event_Does_Not_Exist()
        {
            // Arrange
            await ResetDatabaseAsync();

            // Act
            var response = await _anonymousClient.GetAsync($"/events/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


        [Fact]
        [Trait("Category", "GetAllEvents")]
        public async Task GetAllEvents_Should_Return_Paginated_Results_With_Default_Params()
        {
            // Arrange
            await ResetDatabaseAsync();
            using var client = CreateAdminClient();
            var startAt = DateTime.UtcNow.AddDays(5);

            // Создаём 3 события
            for (var i = 1; i <= 3; i++)
            {
                await client.PostAsJsonAsync("/events", new CreateEventRequest
                {
                    Title = $"Событие {i}",
                    StartAt = startAt.AddDays(i),
                    EndAt = startAt.AddDays(i).AddHours(2),
                    TotalSeats = 10 * i
                }, cancellationToken: TestContext.Current.CancellationToken);
            }

            // Act
            var response = await _anonymousClient.GetAsync("/events?page=1&pageSize=2", TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<PaginatedResultResponse>(cancellationToken: TestContext.Current.CancellationToken);
            body.Should().NotBeNull();
            body!.EventsTotalCount.Should().Be(3);
            body.CurrentPageNumber.Should().Be(1);
            body.EventsCountOnCurrentPage.Should().Be(2);
            body.Events.Should().HaveCount(2);
        }

        [Fact]
        [Trait("Category", "GetAllEvents")]
        public async Task GetAllEvents_Should_Filter_By_Title()
        {
            // Arrange
            await ResetDatabaseAsync();
            using var client = CreateAdminClient();
            var startAt = DateTime.UtcNow.AddDays(5);

            await client.PostAsJsonAsync("/events", new CreateEventRequest
            {
                Title = "Воркшоп без боли",
                StartAt = startAt,
                EndAt = startAt.AddHours(3),
                TotalSeats = 20
            }, cancellationToken: TestContext.Current.CancellationToken);
            await client.PostAsJsonAsync("/events", new CreateEventRequest
            {
                Title = "Воркшоп с болью",
                StartAt = startAt.AddDays(1),
                EndAt = startAt.AddDays(1).AddHours(3),
                TotalSeats = 15
            }, cancellationToken: TestContext.Current.CancellationToken);

            // Act — ищем только события с "без" в названии
            var response = await _anonymousClient.GetAsync("/events?title=без", TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<PaginatedResultResponse>(cancellationToken: TestContext.Current.CancellationToken);
            body.Should().NotBeNull();
            body!.EventsTotalCount.Should().Be(1);
            body.Events.Should().ContainSingle();
            body.Events[0].Title.Should().Be("Воркшоп без боли");
        }

        [Fact]
        [Trait("Category", "GetAllEvents")]
        public async Task GetAllEvents_Should_Return_Empty_When_No_Events_Match()
        {
            // Arrange
            await ResetDatabaseAsync();

            // Act
            var response = await _anonymousClient.GetAsync("/events?title=nonexistent", TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<PaginatedResultResponse>(cancellationToken: TestContext.Current.CancellationToken);
            body.Should().NotBeNull();
            body!.EventsTotalCount.Should().Be(0);
            body.Events.Should().BeEmpty();
        }
    }
}

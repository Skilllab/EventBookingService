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

using EventForge.Booking.Application.DTO;
using EventForge.Booking.Infrastructure.Context;
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

namespace EventForge.Booking.e2eTests
{
    public class E2ECreateBookingTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("eventforge_booking_e2e")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        private WebApplicationFactory<Program> _factory = null!;
        private HttpClient _client = null!;

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
                            ["JwtSettings:SchemeName"] = "Bearer",
                            ["BookingOptions:MaxBookingCount"] = "3"
                        });
                    });
                });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Миграция/создание — один раз при старте класса
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BookingDbContext>>();
            await using var ctx = await db.CreateDbContextAsync();
            await ctx.Database.EnsureCreatedAsync();  // один раз
        }

        public async ValueTask DisposeAsync()
        {
            _client.Dispose();
            await _factory.DisposeAsync();
            await _dbContainer.DisposeAsync();
        }

        private static string GenerateToken(Guid userId, RoleType role)
        {

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("role", role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
            var token = new JwtSecurityToken(
                issuer: JwtIssuer, audience: JwtAudience,
                claims: claims, notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private void SetAuth(Guid userId, RoleType role)
            => _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", GenerateToken(userId, role));

        private void SetAdminAuth(Guid userId)
            => SetAuth(userId, RoleType.Admin);

        private void SetUserAuth(Guid userId)
            => SetAuth(userId, RoleType.User);

        private void ClearAuth()
            => _client.DefaultRequestHeaders.Authorization = null;

        private static Guid NewUserId() => Guid.NewGuid();

        private async Task ResetDatabaseAsync()
        {
            // Только чистим данные
            await using var scope = _factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BookingDbContext>>();
            await using var ctx = await db.CreateDbContextAsync();
            await ctx.Database.ExecuteSqlRawAsync(
                "TRUNCATE TABLE \"Booking\".\"Bookings\" RESTART IDENTITY CASCADE");
        }


        [Fact]
        public async Task CreateBooking_Should_Return_Accepted_When_Valid_Token()
        {
            await ResetDatabaseAsync();
            var userId = NewUserId();
            SetUserAuth(userId);

            var response = await _client.PostAsync($"/bookings/{Guid.NewGuid()}", null, TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            var body = await response.Content.ReadFromJsonAsync<BookingInfoDTO>(cancellationToken: TestContext.Current.CancellationToken);
            body.Should().NotBeNull();
            body!.ID.Should().NotBeEmpty();
            body.Status.Should().Be("Pending");
        }

        [Fact]
        public async Task CreateBooking_Should_Return_Unauthorized_When_No_Token()
        {
            await ResetDatabaseAsync();
            ClearAuth();

            var response = await _client.PostAsync($"/bookings/{Guid.NewGuid()}", null, TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateBooking_Should_Fail_When_MaxBookingCount_Exceeded()
        {
            await ResetDatabaseAsync();
            var userId = NewUserId();
            SetUserAuth(userId);
            var eventId = Guid.NewGuid();

            for (var i = 0; i < 3; i++)
            {
                var ok = await _client.PostAsync($"/bookings/{eventId}", null, TestContext.Current.CancellationToken);
                ok.StatusCode.Should().Be(HttpStatusCode.Accepted);
            }

            var response = await _client.PostAsync($"/bookings/{eventId}", null, TestContext.Current.CancellationToken);
            response.StatusCode.Should().NotBe(HttpStatusCode.Accepted);
        }
    }

}

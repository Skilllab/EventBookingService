using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

using EventForge.Settings.JWT;
using EventForge.Users.Infrastructure.Services;

using FluentAssertions;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Xunit;

namespace EventForge.Users.UnitTests;

public class JwtTokenGeneratorTests
{
    [Fact]
    [Trait("Category", "Jwt")]
    public void GenerateToken_Should_Contain_Sub_Claim()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2025, 7, 1, 10, 30, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(now);
        var options = Options.Create(new JwtSettings
        {
            Secret = "super-secret-key-with-sufficient-length-12345",
            Issuer = "eventforge-users",
            Audience = "eventforge-clients",
            Lifetime = 2,
            SchemeName = null,
        });

        var jwtTokenGenerator = new JwtTokenGenerator(options, fakeTimeProvider);

        // Act
        var token = jwtTokenGenerator.GenerateToken(userId, "User");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        jwt.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(userId.ToString());
    }

    [Fact]
    [Trait("Category", "Jwt")]
    public void GenerateToken_Should_Contain_Role_Claim()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 7, 1, 10, 30, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(now);
        var options = Options.Create(new JwtSettings
        {
            Secret = "super-secret-key-with-sufficient-length-12345",
            Issuer = "eventforge-users",
            Audience = "eventforge-clients",
            Lifetime = 2,
            SchemeName = null,
        });

        var jwtTokenGenerator = new JwtTokenGenerator(options, fakeTimeProvider);

        // Act
        var token = jwtTokenGenerator.GenerateToken(Guid.NewGuid(), "Admin");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        jwt.Claims.First(x => x.Type == "role").Value.Should().Be("Admin");
    }

    [Fact]
    [Trait("Category", "Jwt")]
    public void GenerateToken_Should_Set_Issuer_And_Audience()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 7, 1, 10, 30, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(now);
        var options = Options.Create(new JwtSettings
        {
            Secret = "super-secret-key-with-sufficient-length-12345",
            Issuer = "eventforge-users",
            Audience = "eventforge-clients",
            Lifetime = 2,
            SchemeName = null,
        });

        var jwtTokenGenerator = new JwtTokenGenerator(options, fakeTimeProvider);

        // Act
        var token = jwtTokenGenerator.GenerateToken(Guid.NewGuid(), "User");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        jwt.Issuer.Should().Be("eventforge-users");
        jwt.Audiences.Should().ContainSingle().Which.Should().Be("eventforge-clients");
    }

    [Fact]
    [Trait("Category", "Jwt")]
    public void GenerateToken_Should_Respect_Expiration()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 7, 1, 10, 30, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(now);
        var options = Options.Create(new JwtSettings
        {
            Secret = "super-secret-key-with-sufficient-length-12345",
            Issuer = "eventforge-users",
            Audience = "eventforge-clients",
            Lifetime = 2,
            SchemeName = null,
        });
        var jwtTokenGenerator = new JwtTokenGenerator(options, fakeTimeProvider);

        // Act
        var token = jwtTokenGenerator.GenerateToken(Guid.NewGuid(), "User");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        jwt.ValidFrom.Should().Be(now.UtcDateTime);
        jwt.ValidTo.Should().Be(now.AddHours(2).UtcDateTime);
    }


    [Fact]
    [Trait("Category", "Jwt")]
    public void GenerateToken_Should_Create_Token_With_Expected_Claims_And_Metadata()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2025, 7, 1, 10, 30, 0, TimeSpan.Zero);
        var fakeTimeProvider = new FakeTimeProvider(now);
        var options = Options.Create(new JwtSettings
        {
            Secret = "super-secret-key-with-sufficient-length-12345",
            Issuer = "eventforge-users",
            Audience = "eventforge-clients",
            Lifetime = 2,
            SchemeName = null,
        });
        var jwtTokenGenerator = new JwtTokenGenerator(options, fakeTimeProvider);

        // Act
        var token = jwtTokenGenerator.GenerateToken(userId, "Admin");
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Assert
        jwt.Subject.Should().Be(userId.ToString());
        jwt.Claims.First(x => x.Type == "role").Value.Should().Be("Admin");
        jwt.Issuer.Should().Be("eventforge-users");
        jwt.Audiences.Should().ContainSingle().Which.Should().Be("eventforge-clients");
        jwt.ValidFrom.Should().Be(now.UtcDateTime);
        jwt.ValidTo.Should().Be(now.AddHours(2).UtcDateTime);
        jwt.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.Jti && !string.IsNullOrWhiteSpace(x.Value));
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using EventForge.Settings.JWT;
using EventForge.Users.Application.Interfaces;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventForge.Users.Infrastructure.Services;

/// <summary>
/// Компонент генерации JWT-токенов
/// </summary>
/// <param name="options">Параметры JWT</param>
/// <param name="timeProvider">Провайдер времени</param>
public class JwtTokenGenerator(IOptions<JwtSettings> options, TimeProvider timeProvider) : IJwtTokenGenerator
{
    private readonly JwtSettings _settings = options.Value;

    public string GenerateToken(Guid id, string role)
    {
        var now = timeProvider.GetUtcNow();
        var lifeTime = now.AddHours(_settings.Lifetime);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
            new Claim("role", role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            NotBefore = now.UtcDateTime,
            Expires = lifeTime.UtcDateTime,
            IssuedAt = now.UtcDateTime,
            SigningCredentials = creds,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(securityToken);
    }
}

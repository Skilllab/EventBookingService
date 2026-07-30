using System.Text;

using EventForge.ExceptionMiddleware;
using EventForge.Settings.JWT;
using EventForge.Shared.Constants;
using EventForge.Swagger;
using EventForge.Users.Domain.Exceptions;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace EventForge.Users.Presentation;

/// <summary>
/// Класс расширения для регистрации сервисов Presentation-слоя
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет зависимости слоя Presentation в контейнер зависимостей
    /// </summary>
    /// <param name="services">Коллекция сервисов для регистрации зависимостей</param>
    /// <param name="configuration">Конфигурация приложения</param>
    /// <returns>Обновленная коллекция сервисов</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSharedSwagger("EventForge Users API");

        var jwtOptions = configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>();
        var schemeName = jwtOptions?.SchemeName ?? JwtBearerDefaults.AuthenticationScheme;

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = schemeName;
                options.DefaultChallengeScheme = schemeName;
            })
            .AddJwtBearer(schemeName, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    RoleClaimType = "role",
                    NameClaimType = "sub",
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions?.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions?.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions?.Secret ?? string.Empty)),
                    ClockSkew = TimeSpan.FromMinutes(5),
                };

                options.MapInboundClaims = false;
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(StringConstants.CustomJwtPolicy, policy =>
                policy.AddAuthenticationSchemes(schemeName)
                    .RequireAuthenticatedUser());
        });

        services.Configure<ExceptionHandlingOptions>(options =>
        {
            options.ExceptionHandler = exception => exception switch
            {
                ValidationCustomException nfe => (
                    StatusCodes.Status400BadRequest,
                    new ProblemDetails
                    {
                        Type = nfe.EntityName,
                        Instance = nfe.EntityId,
                        Status = StatusCodes.Status400BadRequest,
                        Detail = nfe.Message
                    }
                ),

                // ... другие исключения сервиса
                _ => (StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = exception.Message })
            };
        });

        return services;
    }
}

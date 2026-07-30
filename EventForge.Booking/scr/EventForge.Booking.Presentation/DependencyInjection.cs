using System.Text;

using EventForge.Booking.Domain.Exceptions;
using EventForge.ExceptionMiddleware;
using EventForge.Settings.JWT;
using EventForge.Shared.Constants;
using EventForge.Swagger;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace EventForge.Booking.Presentation;

/// <summary>
/// Класс расширения для регистрации сервисов Presentation-слоя
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует зависимости Presentation-слоя
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения</param>
    /// <param name="configuration">Конфигурация приложения</param>
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSharedSwagger("EventForge Booking API");

        var jwtOptions = configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>();
        var schemeName = jwtOptions?.SchemeName ?? JwtBearerDefaults.AuthenticationScheme;


        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
                    ClockSkew = TimeSpan.Zero,
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
                NotFoundException nfe => (
                    StatusCodes.Status404NotFound,
                    new ProblemDetails
                    {
                        Type = nfe.EntityName,
                        Instance = nfe.EntityId,
                        Status = StatusCodes.Status404NotFound,
                        Detail = nfe.Message
                    }
                ),

                BookingPastEventException bpee => (
                    StatusCodes.Status400BadRequest,
                    new ProblemDetails
                    {
                        Type = bpee.EntityName,
                        Instance = bpee.EntityId,
                        Status = StatusCodes.Status400BadRequest,
                        Detail = bpee.Message
                    }
                ),

                BookingLimitExceededException bpee2 => (
                    StatusCodes.Status409Conflict,
                    new ProblemDetails
                    {
                        Type = bpee2.EntityName,
                        Instance = bpee2.EntityId,
                        Status = StatusCodes.Status409Conflict,
                        Detail = bpee2.Message
                    }
                ),

                NoAvailableSeatsException bpee3 => (
                    StatusCodes.Status409Conflict,
                    new ProblemDetails
                    {
                        Type = bpee3.EntityName,
                        Instance = bpee3.EntityId,
                        Status = StatusCodes.Status409Conflict,
                        Detail = bpee3.Message
                    }
                ),

                InsufficientPermissionsException bpee4 => (
                    StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Type = bpee4.EntityName,
                    Instance = bpee4.EntityId,
                    Status = StatusCodes.Status403Forbidden,
                    Detail = bpee4.Message
                }
                    ),

                // ... другие исключения сервиса
                _ => (StatusCodes.Status500InternalServerError, new ProblemDetails { Detail = exception.Message })
            };
        });

        return services;
    }
}

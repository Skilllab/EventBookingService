using EventForge.Behaviors.Behaviors;

using MediatR;

using EventForge.Users.Application.CQRS.Commands;
using EventForge.Users.Application.CQRS.Handlers;
using EventForge.Users.Application.CQRS.Queries;
using EventForge.Users.Application.Interfaces;
using EventForge.Users.Application.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventForge.Users.Application;

/// <summary>
/// Класс расширения для регистрации сервисов Application-слоя
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISender, Mediator>();

        services.AddScoped<IRequestHandler<RegisterUserCommand, bool>, RegisterUserCommandHandler>();
        services.AddScoped<IRequestHandler<LoginUserQuery, string?>, LoginUserQueryHandler>();

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));

        return services;
    }
}

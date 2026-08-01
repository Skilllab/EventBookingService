using System;

using EventForge.Behaviors.Behaviors;

using MediatR;

using EventForge.Events.Application.CQRS.Commands;
using EventForge.Events.Application.CQRS.Handlers;
using EventForge.Events.Application.CQRS.Queries;
using EventForge.Events.Application.DTO;
using EventForge.Events.Application.Entities;
using EventForge.Events.Application.Interfaces;
using EventForge.Events.Application.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventForge.Events.Application;

/// <summary>
/// Регистрация зависимостей слоя Application
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует зависимости слоя Application
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ISender, Mediator>();

        services.AddScoped<IRequestHandler<GetEventsQuery, PaginatedResultDTO>, GetEventsQueryHandler>();
        services.AddScoped<IRequestHandler<GetTop10EventsQuery, PaginatedResultTop10DTO>, GetTop10EventsQueryHandler>();
        services.AddScoped<IRequestHandler<GetEventByIdQuery, EventDTO>, GetEventByIdQueryHandler>();
        services.AddScoped<IRequestHandler<CreateEventCommand, EventDTO>, CreateEventCommandHandler>();
        services.AddScoped<IRequestHandler<ChangeEventCommand, bool>, ChangeEventCommandHandler>();
        services.AddScoped<IRequestHandler<CancelEventCommand, bool>, CancelEventCommandHandler>();

        services.Configure<RedisOptions>(configuration.GetSection(nameof(RedisOptions)));

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));


        return services;
    }
}

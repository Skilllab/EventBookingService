using EventForge.Booking.Application.Common;
using EventForge.Booking.Application.CQRS.Commands;
using EventForge.Booking.Application.CQRS.Handlers;
using EventForge.Booking.Application.CQRS.Queries;
using EventForge.Booking.Application.CQRS.Validators;
using EventForge.Booking.Application.DTO;
using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Application.Services;
using EventForge.CQRS;
using EventForge.CQRS.Behaviors;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventForge.Booking.Application;

/// <summary>
/// Регистрация зависимостей слоя Application
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.Configure<BookingOptions>(configuration.GetSection(nameof(BookingOptions)));

        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<ISender, Mediator>();
        services.AddScoped<IRequestHandler<CreateBookingCommand, BookingInfoDTO>, CreateBookingHandler>();
        services.AddScoped<IRequestHandler<CancelBookingCommand, bool>, CancelBookingHandler>();
        services.AddScoped<IRequestHandler<GetBookingByIdQuery, BookingInfoDTO>, GetBookingByIdHandler>();
        services.AddScoped<IRequestHandler<GetAllBookingsQuery, List<BookingInfoDTO>>, GetAllBookingsHandler>();

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));

        services.AddScoped<IRequestValidator<CreateBookingCommand>, CreateBookingCommandValidator>();
        services.AddScoped<IRequestValidator<CancelBookingCommand>, CancelBookingCommandValidator>();


        return services;
    }
}

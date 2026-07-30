
using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Infrastructure.Context;
using EventForge.Booking.Infrastructure.Entities;
using EventForge.Booking.Infrastructure.Repositories;
using EventForge.Booking.Infrastructure.Services;
using EventForge.LoggingDBInterceptor;
using EventForge.Settings.JWT;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


namespace EventForge.Booking.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {

        const string serviceName = "EventForge.Booking";
        const string serviceVersion = "1.0.0";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(serviceName)
                    .AddAspNetCoreInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        // Jaeger по умолчанию принимает OTLP/gRPC на порту 4317
                        options.Endpoint = new Uri(configuration["Otlp:Endpoint"] ?? "http://localhost:4317");
                    });
            })
            .WithLogging(logging =>
            {
                logging.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter();
            });

        services.AddSingleton<LoggingInterceptor>();

        services.AddDbContextFactory<BookingDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"));
        });

        services.Configure<JwtSettings>(
            configuration.GetSection(nameof(JwtSettings)));
        services.Configure<KafkaOptions>(
            configuration.GetSection(nameof(KafkaOptions)));

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();

        // Kafka publisher (Singleton — продюсер переиспользуется)
        services.AddSingleton<IBookingPublisher, KafkaBookingPublisher>();

        // Фоновая публикация сообщений из outbox
        services.AddHostedService<OutboxPublisherBackgroundService>();

        // Consumer-ы входящих сообщений
        services.AddHostedService<BookingConfirmedConsumer>();
        services.AddHostedService<BookingRejectedConsumer>();
        services.AddHostedService<BookingNotApprovedConsumer>();

        return services;
    }
}

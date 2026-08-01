using System;

using EventForge.Events.Application.Interfaces;
using EventForge.Events.Infrastructure.Context;
using EventForge.Events.Infrastructure.Entities;
using EventForge.Events.Infrastructure.Repositories;
using EventForge.Events.Infrastructure.Services;
using EventForge.LoggingDBInterceptor;
using EventForge.Settings.JWT;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using StackExchange.Redis;

namespace EventForge.Events.Infrastructure;

/// <summary>
/// Регистрация зависимостей слоя Infrastructure
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        const string serviceName = "EventForge.Events";
        const string serviceVersion = "1.0.0";

        // Регистрируем сами проверки здоровья и привязываем их к инфраструктуре
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("DefaultConnection")!)
            .AddRedis(configuration.GetConnectionString("Redis")!)
            .AddKafka(setup =>
            {
                setup.BootstrapServers = configuration["KafkaOptions:BootstrapServers"]!;
            }, name: "kafka");

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
                    .AddMeter("EventForge.Events.Kafka")
                    .AddPrometheusExporter();
            });


        services.AddSingleton<LoggingInterceptor>();

        services.AddDbContextFactory<EventsDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));
        services.Configure<KafkaOptions>(configuration.GetSection(nameof(KafkaOptions)));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        services.AddSingleton<KafkaMetrics>();


        services.AddSingleton<BookingRequestedMessageProcessor>();
        services.AddSingleton<BookingRequestedDbRetryPolicy>();

        services.AddHostedService<BookingRequestedConsumer>();
        services.AddHostedService<BookingCancelledConsumer>();
        services.AddHostedService<BookingRequestedRetryConsumer>(); // NEW
        services.AddHostedService<OutboxPublisherBackgroundService>();


        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(configuration.GetConnectionString("Redis") ?? string.Empty);
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 3;
            options.ConnectTimeout = 5000; // Тайм-аут подключения, мс
            options.SyncTimeout = 3000;      // Тайм-аут синхронных операций, мс

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();

        return services;
    }
}

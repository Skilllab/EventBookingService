using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EventForge.CQRS.Behaviors;

/// <summary>
/// Поведение конвейера обработки запросов для сбора метрик
/// </summary>
/// <typeparam name="TRequest">Тип запроса</typeparam>
/// <typeparam name="TResponse">Тип ответа</typeparam>
public sealed class MetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly Meter Meter = new("EventForge.CQRS", "1.0.0");
    private static readonly Counter<long> RequestsTotal = Meter.CreateCounter<long>("cqrs_requests_total");
    private static readonly Histogram<double> DurationMs = Meter.CreateHistogram<double>("cqrs_request_duration_ms", "ms");

    public async Task<TResponse> Handle(TRequest request, CancellationToken ct, Func<Task<TResponse>> next)
    {
        var name = typeof(TRequest).Name;
        var started = Stopwatch.GetTimestamp();
        var success = false;

        try
        {
            var response = await next();
            success = true;
            return response;
        }
        finally
        {
            RequestsTotal.Add(1, new("request", name), new("result", success ? "success" : "error"));
            DurationMs.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds, new KeyValuePair<string, object?>("request", name));
        }
    }
}

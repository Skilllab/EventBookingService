using System.Diagnostics;
using System.Text;

using Confluent.Kafka;

namespace EventForge.Events.Infrastructure.Services;

internal static class KafkaTraceContext
{
    private const string TraceParentHeader = "traceparent";
    private const string TraceStateHeader = "tracestate";

    internal static readonly ActivitySource Source = new("EventForge.Events");

    public static void InjectCurrentContext(Headers headers)
    {
        var activity = Activity.Current;
        if (activity is null)
            return;

        headers.Add(TraceParentHeader, Encoding.UTF8.GetBytes(activity.Id!));

        if (!string.IsNullOrWhiteSpace(activity.TraceStateString))
        {
            headers.Add(TraceStateHeader, Encoding.UTF8.GetBytes(activity.TraceStateString));
        }
    }

    public static ActivityContext ExtractFromHeaders(Headers? headers)
    {
        var traceParent = GetHeader(headers, TraceParentHeader);
        var traceState = GetHeader(headers, TraceStateHeader);

        return CreateContext(traceParent, traceState);
    }

    public static ActivityContext ExtractFromOutbox(string? traceParent, string? traceState)
        => CreateContext(traceParent, traceState);

    private static ActivityContext CreateContext(string? traceParent, string? traceState)
    {
        if (!string.IsNullOrWhiteSpace(traceParent) &&
            ActivityContext.TryParse(traceParent, traceState, out var parent))
        {
            return parent;
        }

        return default;
    }

    private static string? GetHeader(Headers? headers, string name)
    {
        if (headers is null || !headers.TryGetLastBytes(name, out var bytes) || bytes is null)
            return null;

        return Encoding.UTF8.GetString(bytes);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;

using EventForge.Events.Infrastructure.Entities;

using Polly;
using Polly.Retry;


using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventForge.Events.Infrastructure.Services;

/// <summary>
/// Polly-политика локальных retry
/// </summary>
public sealed class BookingRequestedDbRetryPolicy(
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<BookingRequestedDbRetryPolicy> logger)
{
    private readonly ResiliencePipeline _pipeline = BuildPipeline(kafkaOptions.Value, logger);

    private static ResiliencePipeline BuildPipeline(KafkaOptions options, ILogger logger)
    {
        var builder = new ResiliencePipelineBuilder();

        if (options.InPlaceRetryCount <= 0)
            return builder.Build();

        builder.AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<DbUpdateException>(),
            MaxRetryAttempts = options.InPlaceRetryCount,
            Delay = TimeSpan.FromMilliseconds(Math.Max(50, options.InPlaceRetryBaseDelayMs)),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            OnRetry = args =>
            {
                logger.LogWarning(
                    args.Outcome.Exception,
                    "Polly retry attempt={Attempt}",
                    args.AttemptNumber + 1);
                return default;
            }
        });

        return builder.Build();
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct) =>
        await _pipeline.ExecuteAsync(async token => await action(token), ct);
}

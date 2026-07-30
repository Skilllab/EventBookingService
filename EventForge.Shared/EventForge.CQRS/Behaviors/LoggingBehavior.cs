using Microsoft.Extensions.Logging;

namespace EventForge.CQRS.Behaviors;

/// <summary>
/// Поведение конвейера обработки запросов для логирования
/// </summary>
/// <typeparam name="TRequest">Тип запроса</typeparam>
/// <typeparam name="TResponse">Тип ответа</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, CancellationToken ct, Func<Task<TResponse>> next)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("CQRS start {RequestName}", name);
        try
        {
            var result = await next();
            logger.LogInformation("CQRS success {RequestName}", name);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CQRS failed {RequestName}", name);
            throw;
        }
    }
}

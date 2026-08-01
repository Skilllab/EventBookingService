using System;
using System.Threading;
using System.Threading.Tasks;

using MediatR;

using Microsoft.Extensions.Logging;

namespace EventForge.Behaviors.Behaviors;

/// <summary>
/// Поведение конвейера обработки запросов для логирования
/// </summary>
public sealed class LoggingBehavior<TReq, TRes>(ILogger<LoggingBehavior<TReq, TRes>> logger) : IPipelineBehavior<TReq, TRes> where TReq : IRequest<TRes>
{
    public async Task<TRes> Handle(TReq req, RequestHandlerDelegate<TRes> next, CancellationToken ct)
    {
        logger.LogInformation("CQRS start {Name}", typeof(TReq).Name);
        try
        {
            var result = await next();  // ← делегат без параметров
            logger.LogInformation("CQRS success {Name}", typeof(TReq).Name);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CQRS failed {Name}", typeof(TReq).Name);
            throw;
        }
    }
}

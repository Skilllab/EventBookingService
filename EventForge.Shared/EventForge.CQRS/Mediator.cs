using System.Reflection;

using EventForge.CQRS.Behaviors;

using Microsoft.Extensions.DependencyInjection;

namespace EventForge.CQRS;

/// <summary>
/// Класс для отправки команд и запросов в CQRS
/// </summary>
public sealed class Mediator(IServiceProvider serviceProvider) : ISender
{
    /// <summary>
    /// Отправляет команду или запрос и возвращает результат выполнения
    /// </summary>
    /// <typeparam name="TResponse">Тип возвращаемого результата</typeparam>
    /// <param name="request">Команда или запрос для отправки</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат выполнения команды или запроса</returns>
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        var requestType = request.GetType();

        var method = typeof(Mediator)
            .GetMethod(nameof(SendInternal), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(requestType, typeof(TResponse));

        return (Task<TResponse>) method.Invoke(this, new object[] { request, ct })!;
    }

    private Task<TResponse> SendInternal<TRequest, TResponse>(
        IRequest<TResponse> request,
        CancellationToken ct)
        where TRequest : IRequest<TResponse>
    {
        var typedRequest = (TRequest) request;

        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .ToArray();

        Func<Task<TResponse>> next = () => handler.Handle(typedRequest, ct);

        foreach (var behavior in behaviors)
        {
            var currentNext = next;
            next = () => behavior.Handle(typedRequest, ct, currentNext);
        }

        return next();
    }
}

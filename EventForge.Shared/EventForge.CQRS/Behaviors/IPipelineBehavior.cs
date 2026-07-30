namespace EventForge.CQRS.Behaviors;

/// <summary>
/// Интерфейс для реализации поведения конвейера обработки запросов
/// </summary>
/// <typeparam name="TRequest">Тип запроса</typeparam>
/// <typeparam name="TResponse">Тип ответа</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct, Func<Task<TResponse>> next);
}

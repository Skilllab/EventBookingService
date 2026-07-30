namespace EventForge.CQRS.Behaviors;

/// <summary>
/// Поведение конвейера обработки запросов для валидации
/// </summary>
/// <typeparam name="TRequest">Тип запроса</typeparam>
/// <typeparam name="TResponse">Тип ответа</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IRequestValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, CancellationToken ct, Func<Task<TResponse>> next)
    {
        foreach (var validator in validators)
            validator.Validate(request);

        return next();
    }
}

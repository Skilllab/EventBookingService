using FluentValidation;

using MediatR;

namespace EventForge.Behaviors.Behaviors;

/// <summary>
/// Поведение конвейера обработки запросов для валидации
/// </summary>
/// <typeparam name="TRequest">Тип запроса</typeparam>
/// <typeparam name="TResponse">Тип ответа</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators) // 2. Меняем на IValidator<TRequest>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // 3. Делаем метод асинхронным (async/await)
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Если валидаторов для данного запроса нет — сразу идем дальше
        if (!validators.Any())
        {
            return await next();
        }

        // Создаем контекст валидации FluentValidation
        var context = new ValidationContext<TRequest>(request);

        // Перебираем валидаторы. Так как у вас везде переопределен метод Validate, 
        // при первой же ошибке внутри валидатора выбросится ваше кастомное исключение.
        foreach (var validator in validators)
        {
            await validator.ValidateAsync(context, ct);
        }

        return await next();
    }
}

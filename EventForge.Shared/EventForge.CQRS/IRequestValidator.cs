namespace EventForge.CQRS;

/// <summary>
/// Интерфейс для реализации поведения конвейера обработки запросов
/// </summary>
/// <typeparam name="TRequest"></typeparam>
public interface IRequestValidator<in TRequest>
{
    void Validate(TRequest request);
}

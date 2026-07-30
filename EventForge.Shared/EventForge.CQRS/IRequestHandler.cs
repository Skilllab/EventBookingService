namespace EventForge.CQRS;

/// <summary>
/// Интерфейс для обработки команд и запросов в CQRS
/// </summary>
/// <typeparam name="TRequest">Тип команды или запроса</typeparam>
/// <typeparam name="TResponse">Тип возвращаемого результата</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Обрабатывает команду или запрос и возвращает результат выполнения
    /// </summary>
    /// <param name="request">Команда или запрос для обработки</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат выполнения команды или запроса</returns>
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}

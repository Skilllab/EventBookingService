namespace EventForge.CQRS;

/// <summary>
/// Интерфейс для отправки команд и запросов в CQRS
/// </summary>
public interface ISender
{
    /// <summary>
    /// Отправляет команду и возвращает результат выполнения
    /// </summary>
    /// <typeparam name="TResponse">Тип возвращаемого результата</typeparam>
    /// <param name="request">Команда или запрос для отправки</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат выполнения команды или запроса</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
}

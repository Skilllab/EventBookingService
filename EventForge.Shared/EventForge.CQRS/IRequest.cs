namespace EventForge.CQRS;

/// <summary>
/// Интерфейс для команд и запросов в CQRS
/// </summary>
/// <typeparam name="TResponse">Тип возвращаемого результата</typeparam>
public interface IRequest<out TResponse>;

namespace EventForge.Events.Application.Interfaces;

/// <summary>
/// Репозиторий хранения факта обработки сообщений
/// </summary>
public interface IProcessedMessageRepository
{
    /// <summary>
    /// Проверяет, было ли сообщение с указанным идентификатором уже обработано
    /// </summary>
    /// <param name="id">Идентификатор сообщения</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Добавляет запись об обработанном сообщении
    /// </summary>
    /// <param name="id">Идентификатор сообщения</param>
    /// <param name="messageType">Тип сообщения</param>
    Task AddAsync(Guid id, string messageType, CancellationToken ct);
}

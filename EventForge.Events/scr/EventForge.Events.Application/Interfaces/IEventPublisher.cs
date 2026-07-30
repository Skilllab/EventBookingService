namespace EventForge.Events.Application.Interfaces;

/// <summary>
/// Контракт публикации события подтвержденной брони.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Публикует сериализованный payload
    /// </summary>
    /// <param name="topic">Топик для публикации</param>
    /// <param name="key">Ключ сообщения</param>
    /// <param name="payload">Сериализованный payload</param>
    /// <param name="ct">Токен отмены</param>
    Task PublishRawAsync(string topic, string key, string payload, CancellationToken ct);
}

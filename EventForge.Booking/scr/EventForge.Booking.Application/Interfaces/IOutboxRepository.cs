using EventForge.Booking.Domain.Entities;

namespace EventForge.Booking.Application.Interfaces;

/// <summary>
/// Репозиторий outbox-сообщений
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Получить группу необработанных outbox-сообщений
    /// </summary>
    /// <param name="batchSize">Размер группы</param>
    /// <param name="ct">Токен отмены</param>
    Task<List<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct);

    /// <summary>
    /// Обновить outbox-сообщение как обработанное
    /// </summary>
    /// <param name="id">Идентификатор сообщения</param>
    /// <param name="ct">Токен отмены</param>
    Task MarkProcessedAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Пометить outbox-сообщение как ошибочное
    /// </summary>
    /// <param name="id">Идентификатор сообщения</param>
    /// <param name="error">Описание ошибки</param>
    /// <param name="ct">Токен отмены</param>
    Task MarkFailedAsync(Guid id, string error, CancellationToken ct);
}

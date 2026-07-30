using EventForge.Booking.Domain.Entities;


namespace EventForge.Booking.Application.Interfaces;

/// <summary>
/// Основной интерфейс репозитория бронирования
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Получение бронирования по ID
    /// </summary>
    /// <param name="id">ID бронирования</param>
    /// <param name="ct">Токен отмены</param>
    Task<BookingModel?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Получение всех бронирований
    /// </summary>
    /// <param name="ct">Токен отмены</param>
    Task<List<BookingModel>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Получение бронирований пользователя со статусами pending и confirmed
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Количество активных бронирований пользователя</returns>
    Task<int> GetUserActiveBookingsCountAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Создать бронирование и интеграционное событие в outbox
    /// </summary>
    /// <param name="booking">Бронирование</param>
    /// <param name="outboxMessage">Сообщение для добавления в outbox</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> CreateAndAddOutboxAsync(BookingModel booking, OutboxMessage outboxMessage, CancellationToken ct);

    /// <summary>
    /// Отменить бронирование создать интеграционное событие в outbox
    /// </summary>
    /// <param name="bookingId">ID бронирования</param>
    /// <param name="userId">ID пользователя</param>
    /// <param name="processedAt">Время обработки бронирования</param>
    /// <param name="outboxMessage">Сообщение для добавления в outbox</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> CancelAndAddOutboxAsync(Guid bookingId, Guid userId, DateTime processedAt, OutboxMessage outboxMessage, CancellationToken ct);

    /// <summary>
    /// Подтверждает бронирование без создания outbox-сообщения
    /// </summary>
    /// <param name="bookingId">ID бронирования.</param>
    /// <param name="processedAt">Время обработки.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<bool> ConfirmBookingAsync(Guid bookingId, DateTime processedAt, CancellationToken ct);

    /// <summary>
    /// Отклоняет бронирование без создания outbox-сообщения
    /// </summary>
    /// <param name="bookingId">ID бронирования.</param>
    /// <param name="processedAt">Время обработки.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<bool> RejectBookingAsync(Guid bookingId, DateTime processedAt, CancellationToken ct);
}

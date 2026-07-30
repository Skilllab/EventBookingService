using EventForge.Booking.Application.DTO;
using EventForge.Shared.Enums;

namespace EventForge.Booking.Application.Interfaces;

/// <summary>
/// Интерфейс сервиса бронирования
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создание бронирования для указанного события
    /// </summary>
    /// <param name="eventId">ID события</param>
    /// <param name="userId">ID пользователя</param>
    /// <param name="ct">Токен отмены</param>
    Task<BookingInfoDTO> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct);

    /// <summary>
    /// Получение бронирования по идентификатору
    /// </summary>
    /// <param name="bookingId">ID бронирования</param>
    /// <param name="ct">Токен отмены</param>
    Task<BookingInfoDTO> GetBookingByIdAsync(Guid bookingId, CancellationToken ct);

    /// <summary>
    /// Отмена бронирования
    /// </summary>
    /// <param name="bookingId">ID бронирования</param>
    /// <param name="userId">ID пользователя</param>
    /// <param name="userRole">Роль пользователя</param>
    /// <param name="ct">Токен отмены</param>
    Task<bool> CancelBooking(Guid bookingId, Guid userId, RoleType userRole, CancellationToken ct);

    /// <summary>
    /// Получение всех бронирований для пользователя с учетом его роли
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="roleType">Роль пользователя</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Список бронирований</returns>
    Task<List<BookingInfoDTO>>GetAllBooking(Guid userId, RoleType  roleType, CancellationToken ct);
}

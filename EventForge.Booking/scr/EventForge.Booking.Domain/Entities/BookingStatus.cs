using System.ComponentModel;

namespace EventForge.Booking.Domain.Entities;

/// <summary>
/// Статус бронирования
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// Бронь создана, ожидает обработки
    /// </summary>
    [Description("Бронь создана, ожидает обработки")]
    Pending = 0,

    /// <summary>
    /// Бронь подтверждена
    /// </summary>
    [Description("Бронь подтверждена")]
    Confirmed = 1,

    /// <summary>
    /// Бронь отклонена
    /// </summary>
    [Description("Бронь отклонена")]
    Rejected = 2,

    /// <summary>
    /// Бронь отменена пользователем
    /// </summary>
    [Description("Бронь отменена")]
    Cancelled = 3,
}

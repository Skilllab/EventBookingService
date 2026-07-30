namespace EventForge.Booking.Application.DTO;

/// <summary>
/// DTO для ответа по созданию бронирования (слой Application)
/// </summary>
/// <param name="ID">Идентификатор бронирования</param>
/// <param name="EventID">Идентификатор события, по которому создано бронирование</param>
/// <param name="Status">Статус бронирования</param>
public sealed record BookingInfoDTO(
    Guid ID,
    Guid EventID,
    string Status
);

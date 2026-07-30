using EventForge.Shared.Exceptions;

namespace EventForge.Booking.Domain.Exceptions;

/// <summary>
/// Исключение, которое выбрасывается в случае если у события нет мест для бронирования
/// </summary>
public class NoAvailableSeatsException : DomainException
{
    public NoAvailableSeatsException(string entityName, string entityId)
        : base($"Для элемента {entityName} c ID: {entityId} нет доступных мест для бронирования", entityName, entityId) { }

    public NoAvailableSeatsException(string entityName, string entityId, string message)
        : base(message, entityName, entityId) { }
}

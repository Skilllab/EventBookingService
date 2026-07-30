using EventForge.Shared.Exceptions;

namespace EventForge.Booking.Domain.Exceptions;

/// <summary>
/// Исключение, которое выбрасывается в случае если превышен лимит активных бронирований для события
/// </summary>
public class BookingLimitExceededException : DomainException
{
    public BookingLimitExceededException(string entityName, string entityId)
        : base($"Превышен лимит активных бронирований для '{entityName}' с ID: '{entityId}'.", entityName, entityId) { }

    public BookingLimitExceededException(string entityName, string entityId, string message)
        : base(message, entityName, entityId) { }
}

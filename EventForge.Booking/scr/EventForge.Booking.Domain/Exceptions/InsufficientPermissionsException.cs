using EventForge.Shared.Exceptions;

namespace EventForge.Booking.Domain.Exceptions;

/// <summary>
/// Исключение, которое выбрасывается в случае если пользователь не имеет прав на выполнение действия
/// </summary>
public class InsufficientPermissionsException : DomainException
{
    public InsufficientPermissionsException(string entityName, string entityId)
        : base($"У '{entityName}' с ID: '{entityId}' недостаточно прав для выполнения операции.", entityName, entityId) { }

    public InsufficientPermissionsException(string entityName, string entityId, string message)
        : base(message, entityName, entityId) { }
}

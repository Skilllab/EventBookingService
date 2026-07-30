using EventForge.Shared.Exceptions;

namespace EventForge.Booking.Domain.Exceptions;

/// <summary>
/// Исключение, которое выбрасывается в случае если сущность не была найдена 
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, string entityId)
        : base($"Элемент {entityName} c ID: '{entityId}' не найден.", entityName, entityId) { }

    public NotFoundException(string entityName, string entityId, string message)
        : base(message, entityName, entityId) { }
}

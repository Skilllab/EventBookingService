using EventForge.Shared.Exceptions;

namespace EventForge.Events.Domain.Exceptions;

/// <summary>
/// Исключение, которое выбрасывается в случае если у сущности есть ошибки валидации 
/// </summary>
public class ValidationCustomException : DomainException
{
    public ValidationCustomException(string entityName, string entityId)
        : base($"Элемент {entityName} c ID: '{entityId}' имеет ошибки валидации.", entityName, entityId) { }

    public ValidationCustomException(string entityName, string entityId, string message)
        : base(entityName, entityId, message) { }
}

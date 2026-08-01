using System;

namespace EventForge.Shared.Exceptions;

/// <summary>
/// Базовый класс для кастом исключений
/// </summary>
/// <param name="message">Сообщение об ошибке</param>
/// <param name="entityName">Имя сущности</param>
/// <param name="entityId">Идентификатор сущности</param>
public abstract class DomainException(string message, string entityName, string entityId) : Exception(message)
{
    /// <summary>
    /// Тип сущности
    /// </summary>
    public string EntityName { get; } = entityName;

    /// <summary>
    /// Идентификатор сущности
    /// </summary>
    public string EntityId { get; } = entityId;

    /// <inheritdoc />
    protected DomainException(string message, Exception innerException)
        : this(message, string.Empty, string.Empty) { }
}

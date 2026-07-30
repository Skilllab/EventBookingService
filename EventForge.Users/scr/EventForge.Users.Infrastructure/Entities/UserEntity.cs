using EventForge.Shared.Enums;

namespace EventForge.Users.Infrastructure.Entities;

/// <summary>
/// Сущность пользователя для хранения в БД
/// </summary>
public class UserEntity
{
    /// <summary>
    /// Уникальный идентификатор пользователя
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Имя входа пользователя
    /// </summary>
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Хэш пароля пользователя
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Роль пользователя
    /// </summary>
    public RoleType Role { get; set; }
}

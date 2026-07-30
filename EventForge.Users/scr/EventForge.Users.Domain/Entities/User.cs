using EventForge.Shared.Enums;
using EventForge.Users.Domain.Exceptions;

namespace EventForge.Users.Domain.Entities;

/// <summary>
/// Модель пользователя
/// </summary>
public class User
{
    /// <summary>
    /// Уникальный идентификатор пользователя
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Имя входа пользователя
    /// </summary>
    public string Login { get; private set; }

    /// <summary>
    /// Хэш пароля пользователя
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// Роль пользователя
    /// </summary>
    public RoleType Role { get; private set; }

    private User(Guid id, string login, string passwordHash, RoleType role)
    {
        Id = id;
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }

    /// <summary>
    /// Создать нового пользователя
    /// </summary>
    /// <param name="login">Имя входа пользователя</param>
    /// <param name="passwordHash">Хэш пароля пользователя</param>
    /// <param name="role">Роль пользователя</param>
    public static User Create(string login, string passwordHash, RoleType role)
    {
        Validate(login, passwordHash);
        return new User(Guid.NewGuid(), login, passwordHash, role);
    }

    /// <summary>
    /// Восстановить пользователя из хранилища
    /// </summary>
    /// <param name="id">Идентификатор пользователя</param>
    /// <param name="login">Имя входа пользователя</param>
    /// <param name="passwordHash">Хэш пароля пользователя</param>
    /// <param name="role">Роль пользователя</param>
    public static User Restore(Guid id, string login, string passwordHash, RoleType role)
    {
        Validate(login, passwordHash);
        return new User(id, login, passwordHash, role);
    }

    /// <summary>
    /// Проверить валидность логина и хэша пароля
    /// </summary>
    /// <param name="login">Имя входа пользователя</param>
    /// <param name="passwordHash">Хэш пароля пользователя</param>
    private static void Validate(string login, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(login))
            throw new ValidationCustomException(nameof(User), Guid.Empty.ToString(), "Логин пользователя не может быть пустым.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ValidationCustomException(nameof(User), Guid.Empty.ToString(), "Пароль пользователя не может быть пустым.");
    }
}

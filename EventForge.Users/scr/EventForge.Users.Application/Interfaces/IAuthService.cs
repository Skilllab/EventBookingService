namespace EventForge.Users.Application.Interfaces;

/// <summary>
/// Сервис регистрации и аутентификации пользователя
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    /// <param name="login">Имя входа (логин)</param>
    /// <param name="password">Пароль</param>
    /// <param name="role">Рол.</param>
    Task<bool> RegisterUserAsync(string login, string password, string? role);

    /// <summary>
    /// Вход пользователя
    /// </summary>
    /// <param name="login">Имя входа (логин)</param>
    /// <param name="password">Пароль.</param>
    Task<string?> LoginUserAsync(string login, string password);
}

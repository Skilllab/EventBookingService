using EventForge.Users.Domain.Entities;

namespace EventForge.Users.Application.Interfaces;

/// <summary>
/// Основной интерфейс репозитория с пользователями
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Получить пользователя по имени входа (логину)
    /// </summary>
    /// <param name="login">Имя входа (логин)</param>
    Task<User?> GetByLoginAsync(string login);

    /// <summary>
    /// Добавить пользователя
    /// </summary>
    /// <param name="user">Доменная модель пользователя</param>
    Task AddAsync(User user);

    /// <summary>
    /// Проверка существования пользователя при регистрации
    /// </summary>
    /// <param name="login">Имя входа (логин)</param>
    Task<bool> ExistsAsync(string login);
}

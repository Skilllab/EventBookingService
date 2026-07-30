using EventForge.Users.Domain.Entities;
using EventForge.Users.Infrastructure.Entities;

namespace EventForge.Users.Infrastructure.Mapping;

/// <summary>
/// Маппинг между доменной моделью пользователя и сущностью БД
/// </summary>
internal static class UserMapper
{
    /// <summary>
    /// Преобразовать доменную модель пользователя в сущность БД
    /// </summary>
    /// <param name="user">Доменная модель пользователя</param>
    /// <returns>Сущность БД пользователя</returns>
    public static UserEntity ToEntity(this User user) =>
        new()
        {
            Id = user.Id,
            Login = user.Login,
            PasswordHash = user.PasswordHash,
            Role = user.Role,
        };

    /// <summary>
    /// Преобразовать сущность БД в доменную модель пользователя
    /// </summary>
    /// <param name="entity">Сущность БД пользователя</param>
    /// <returns>Доменная модель пользователя</returns>
    public static User ToDomain(this UserEntity entity) => User.Restore(entity.Id, entity.Login, entity.PasswordHash, entity.Role);
}

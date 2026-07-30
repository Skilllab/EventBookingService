namespace EventForge.Users.Application.Interfaces;

/// <summary>
/// Интерфейс для генерации JWT токенов
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Генерирует JWT токен для указанного пользователя
    /// </summary>
    /// <param name="id">ID пользователя</param>
    /// <param name="role">Роль пользователя</param>
    string GenerateToken(Guid id, string role);
}

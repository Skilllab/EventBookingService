using EventForge.Shared.Enums;
using EventForge.Users.Application.Interfaces;
using EventForge.Users.Domain.Entities;

namespace EventForge.Users.Application.Services;

/// <summary>
/// Сервис регистрации и входа пользователей
/// </summary>
/// <param name="userRepository">Репозиторий пользователей</param>
/// <param name="passwordHasher">Компонент хэширования паролей</param>
/// <param name="tokenGenerator">Компонент генерации JWT-токенов</param>
public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator) : IAuthService
{
    public async Task<bool> RegisterUserAsync(string login, string password, string? role)
    {
        var enumRole = RoleType.User;

        if (!string.IsNullOrEmpty(role) && Enum.TryParse<RoleType>(role, true, out var parsedRole))
            enumRole = parsedRole;

        if (await userRepository.ExistsAsync(login))
            return false;

        var hash = passwordHasher.HashPassword(password);
        var user = User.Create(login, hash, enumRole);

        await userRepository.AddAsync(user);
        return true;
    }

    public async Task<string?> LoginUserAsync(string login, string password)
    {
        var user = await userRepository.GetByLoginAsync(login);
        if (user is null)
            return null;

        var isPasswordValid = passwordHasher.VerifyPassword(password, user.PasswordHash);
        return !isPasswordValid
            ? null
            : tokenGenerator.GenerateToken(user.Id, user.Role.ToString());
    }
}

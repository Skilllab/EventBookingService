using System.Security.Cryptography;
using System.Text;

using EventForge.Users.Application.Interfaces;

namespace EventForge.Users.Infrastructure.Services;

/// <summary>
/// Компонент для хеширования паролей и проверки соответствия
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Хеширует пароль с использованием SHA-256
    /// </summary>
    /// <param name="password">Исходный пароль</param>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Пароль не может быть пустым", nameof(password));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Проверяет соответствие пароля хешу
    /// </summary>
    /// <param name="password">Пароль для проверки</param>
    /// <param name="hash">Сохраненный хеш</param>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Пароль не может быть пустым", nameof(password));

        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Хеш не может быть пустым", nameof(hash));

        var computedHash = HashPassword(password);
        var b1 = Encoding.UTF8.GetBytes(computedHash);
        var b2 = Encoding.UTF8.GetBytes(hash);
        return CryptographicOperations.FixedTimeEquals(b1, b2);
    }
}

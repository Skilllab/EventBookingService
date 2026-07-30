using System.ComponentModel.DataAnnotations;

namespace EventForge.Users.Presentation.DTO;

/// <summary>
/// Запрос на вход пользователя
/// </summary>
public class LoginDataRequest
{
    /// <summary>
    /// Логин пользователя
    /// </summary>
    [Required(ErrorMessage = "Логин обязателен")]
    [StringLength(64, MinimumLength = 3)]
    public string Login { get; set; } = string.Empty;

    /// <summary>
    /// Пароль пользователя
    /// </summary>
    [Required(ErrorMessage = "Пароль обязателен")]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

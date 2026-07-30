using EventForge.CQRS;
using EventForge.Shared.Enums;
using EventForge.Users.Application.CQRS.Commands;
using EventForge.Users.Domain.Exceptions;

namespace EventForge.Users.Application.CQRS.Validators;

/// <summary>
/// Валидация команды регистрации пользователя на уровне Application.
/// </summary>
public sealed class RegisterUserCommandValidator : IRequestValidator<RegisterUserCommand>
{
    public void Validate(RegisterUserCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.Login))
            throw new ValidationCustomException(nameof(RegisterUserCommand), Guid.Empty.ToString(), "Логин обязателен.");

        if (request.Login.Length is < 3 or > 64)
            throw new ValidationCustomException(nameof(RegisterUserCommand), Guid.Empty.ToString(), "Логин должен быть от 3 до 64 символов.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationCustomException(nameof(RegisterUserCommand), Guid.Empty.ToString(), "Пароль обязателен.");

        if (request.Password.Length < 6)
            throw new ValidationCustomException(nameof(RegisterUserCommand), Guid.Empty.ToString(), "Пароль должен быть не короче 6 символов.");

        if (!string.IsNullOrWhiteSpace(request.Role) &&
            !Enum.TryParse<RoleType>(request.Role, true, out _))
            throw new ValidationCustomException(nameof(RegisterUserCommand), Guid.Empty.ToString(), "Роль указана некорректно.");
    }
}

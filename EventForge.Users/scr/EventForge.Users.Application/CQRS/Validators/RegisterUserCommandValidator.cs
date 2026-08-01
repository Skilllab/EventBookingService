using EventForge.Shared.Enums;
using EventForge.Users.Application.CQRS.Commands;

using FluentValidation;

namespace EventForge.Users.Application.CQRS.Validators;

/// <summary>
/// Валидация команды регистрации пользователя на уровне Application
/// </summary>
public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        // Гарантирует остановку на первой же ошибке (Fail-Fast)
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Login)
            .NotEmpty()
            .WithMessage("Логин обязателен.")
            .Length(3, 64)
            .WithMessage("Логин должен быть от 3 до 64 символов.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Пароль обязателен.")
            .MinimumLength(6)
            .WithMessage("Пароль должен быть не короче 6 символов.");

        RuleFor(x => x.Role)
            .Must(role => Enum.TryParse<RoleType>(role, true, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.Role))
            .WithMessage("Роль указана некорректно.");
    }
}

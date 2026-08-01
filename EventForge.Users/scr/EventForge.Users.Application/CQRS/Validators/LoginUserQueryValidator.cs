using EventForge.Users.Application.CQRS.Queries;

using FluentValidation;

namespace EventForge.Users.Application.CQRS.Validators;

/// <summary>
/// Валидация запроса на вход пользователя на уровне Application
/// </summary>
public sealed class LoginUserQueryValidator : AbstractValidator<LoginUserQuery>
{
    public LoginUserQueryValidator()
    {
        // Прекращает всю валидацию класса, как только ЛЮБОЕ правило вернет ошибку
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Login)
            .NotEmpty() // Проверяет на null, empty и whitespace
            .WithMessage("Login обязателен.");

        RuleFor(x => x.Password)
            .NotEmpty() // Проверяет на null, empty и whitespace
            .WithMessage("Password обязателен.");
    }
}

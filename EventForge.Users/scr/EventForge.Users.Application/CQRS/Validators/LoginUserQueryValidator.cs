using EventForge.CQRS;
using EventForge.Users.Application.CQRS.Queries;

namespace EventForge.Users.Application.CQRS.Validators;

/// <summary>
/// Валидация запроса на вход пользователя на уровне Application
/// </summary>
public sealed class LoginUserQueryValidator : IRequestValidator<LoginUserQuery>
{
    public void Validate(LoginUserQuery request)
    {
        if (string.IsNullOrWhiteSpace(request.Login))
            throw new ArgumentException("Login обязателен.");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password обязателен.");
    }
}

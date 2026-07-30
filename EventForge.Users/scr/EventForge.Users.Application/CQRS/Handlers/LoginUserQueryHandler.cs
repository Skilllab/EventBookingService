using EventForge.CQRS;
using EventForge.Users.Application.CQRS.Queries;
using EventForge.Users.Application.Interfaces;

namespace EventForge.Users.Application.CQRS.Handlers;

/// <summary>
/// Обработчик запроса на вход пользователя
/// </summary>
/// <param name="authService">Сервис аутентификации</param>
public sealed class LoginUserQueryHandler(IAuthService authService)
    : IRequestHandler<LoginUserQuery, string?>
{
    /// <summary>
    /// Обработчик запроса на вход пользователя
    /// </summary>
    /// <param name="request">Запрос на вход пользователя</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат входа пользователя</returns>
    public Task<string?> Handle(LoginUserQuery request, CancellationToken ct) =>
        authService.LoginUserAsync(request.Login, request.Password);
}

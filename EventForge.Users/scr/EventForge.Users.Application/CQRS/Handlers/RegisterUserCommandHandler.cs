using EventForge.CQRS;
using EventForge.Users.Application.CQRS.Commands;
using EventForge.Users.Application.Interfaces;

namespace EventForge.Users.Application.CQRS.Handlers;

/// <summary>
/// Обработчик команды регистрации пользователя
/// </summary>
/// <param name="authService">Сервис аутентификации</param>
public sealed class RegisterUserCommandHandler(IAuthService authService)
    : IRequestHandler<RegisterUserCommand, bool>
{
    /// <summary>
    /// Обработчик команды регистрации пользователя
    /// </summary>
    /// <param name="request">Команда регистрации пользователя</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат регистрации пользователя</returns>
    public Task<bool> Handle(RegisterUserCommand request, CancellationToken ct) =>
        authService.RegisterUserAsync(request.Login, request.Password, request.Role);
}

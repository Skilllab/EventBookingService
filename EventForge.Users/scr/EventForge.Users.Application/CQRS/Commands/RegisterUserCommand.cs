using EventForge.CQRS;

namespace EventForge.Users.Application.CQRS.Commands;

/// <summary>
/// Команда для регистрации нового пользователя
/// </summary>
/// <param name="Login">Логин пользователя</param>
/// <param name="Password">Пароль пользователя</param>
/// <param name="Role">Роль пользователя</param>
public sealed record RegisterUserCommand(string Login, string Password, string? Role) : IRequest<bool>;

using EventForge.CQRS;

namespace EventForge.Users.Application.CQRS.Queries;

/// <summary>
/// Запрос на вход пользователя
/// </summary>
/// <param name="Login">Логин пользователя</param>
/// <param name="Password">Пароль пользователя</param>
public sealed record LoginUserQuery(string Login, string Password) : IRequest<string?>;

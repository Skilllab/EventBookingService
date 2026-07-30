using Asp.Versioning;

using EventForge.CQRS;
using EventForge.Users.Application.CQRS.Commands;
using EventForge.Users.Application.CQRS.Queries;
using EventForge.Users.Presentation.DTO;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

namespace EventForge.Users.Presentation.Controllers;

/// <summary>
/// Контроллер для аутентификации пользователей
/// </summary>
/// <param name="sender">Сервис отправки команд и запросов</param>
/// <param name="logger">Логгер</param>
[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(ISender sender, ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Регистрация нового пользователя с указанными логином, паролем и ролью.
    /// </summary>
    /// <param name="userRequest">
    /// Данные пользователя:
    /// <c>Login</c> - непустая строка, минимум 3 символа
    /// <c>Password</c> - непустая строка, минимум 6 символов.
    /// </param>
    /// <remarks>
    /// Пароль хешируется через BCrypt перед сохранением. В БД попадает только хеш,
    /// исходный пароль не хранится. Эндпоинт доступен анонимно.
    /// </remarks>
    [HttpPost("register")]
    [ApiVersion("1.0")]
    [Tags("API для регистрации")]
    [SwaggerOperation(Summary = "Регистрация пользователя (Register)")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Пользователь успешно зарегистрирован.")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Ошибка регистрации пользователя (логин занят или модель невалидна, пустой логин, короткий пароль и т.д.)")]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest userRequest)
    {
        logger.LogDebug("Обработка запроса POST {methodName}. Регистрация нового пользователя: {login}", nameof(Register), userRequest.Login);

        if (await sender.Send(new RegisterUserCommand(userRequest.Login, userRequest.Password, userRequest.Role)))
            return NoContent();

        return BadRequest(new { message = "Ошибка регистрации пользователя" });
    }

    /// <summary>
    /// Аутентификация пользователя по логину и паролю
    /// </summary>
    /// <param name="request">Данные для входа: логин и пароль.</param>
    /// <remarks>
    /// Токен содержит claims: <c>sub</c> (ID пользователя), <c>role</c> (роль), Login
    /// а также стандартные <c>iat</c>, <c>exp</c>, <c>iss</c>, <c>aud</c>.
    /// Срок жизни токена задаётся в <c>JwtSettings:Lifetime</c> (в часах).
    /// </remarks>
    [HttpPost("login")]
    [ApiVersion("1.0")]
    [Tags("API для аутентификации")]
    [SwaggerOperation(Summary = "Аутентификация пользователя (Login)")]
    [SwaggerResponse(StatusCodes.Status200OK, "объект с JWT-токеном")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "если пользователь не существует или пароль не совпадает")]
    public async Task<IActionResult> Login([FromBody] LoginDataRequest request)
    {
        logger.LogDebug("Обработка запроса POST {methodName}. Аутентификация пользователя: {login}", nameof(Login), request.Login);

        var token = await sender.Send(new LoginUserQuery(request.Login, request.Password));
        if (token is null)
            return NotFound(new { message = "Неверный логин или пароль." });

        return Ok(new { Token = token });
    }
}

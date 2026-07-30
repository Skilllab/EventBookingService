namespace EventForge.Events.Application.Entities;

/// <summary>
/// Класс для хранения настроек Redis
/// </summary>
public class RedisOptions
{
    /// <summary>
    /// Время жизни кэша для одного события в минутах
    /// </summary>
    public int SingleEventExpirationMinutes{ get; set; } = 5;

    /// <summary>
    /// Время жизни кэша для топ 10 событий в минутах
    /// </summary>
    public int TopEventsExpirationMinutes{ get; set; } = 10;


}

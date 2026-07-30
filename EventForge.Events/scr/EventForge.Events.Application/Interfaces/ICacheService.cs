namespace EventForge.Events.Application.Interfaces;

public interface ICacheService
{
    /// <summary>
    /// Получить значение (возвращает null, если ключа нет или кэш недоступен)
    /// </summary>
    /// <param name="key">Ключ значения в кэше</param>
    Task<string?> GetStringAsync(string key);

    /// <summary>
    /// Записать значение со временем жизни (TTL)
    /// </summary>
    /// <param name="key">Ключ значения в кэше</param>
    /// <param name="value">Значение для записи в кэш</param>
    /// <param name="expiration">Время жизни значения в кэше</param>
    Task SetStringAsync(string key, string value, TimeSpan expiration);

    /// <summary>
    /// Удалить значение
    /// </summary>
    /// <param name="key">Ключ значения в кэше</param>
    Task RemoveAsync(string key);
}

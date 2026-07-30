using EventForge.Events.Application.Interfaces;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace EventForge.Events.Infrastructure.Services;

public class RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger) : ICacheService
{
    private IDatabase? GetDatabase()
    {
        try
        {
            // GetDatabase() — легковесный метод, Проверяем при операциях
            return redis.GetDatabase();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении базы данных Redis.");
            return null;
        }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            var db = GetDatabase();
            if (db == null) return null;

            return await db.StringGetAsync(key);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis недоступен при попытке чтения ключа {Key}. Запрос деградирует до БД.", key);
            return null;
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan expiration)
    {
        try
        {
            var db = GetDatabase();
            if (db == null) return;

            await db.StringSetAsync(key, value, expiration);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis недоступен при попытке записи ключа {Key}.", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            var db = GetDatabase();
            if (db == null) return;

            await db.KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis недоступен при попытке удаления ключа {Key}.", key);
        }
    }
}

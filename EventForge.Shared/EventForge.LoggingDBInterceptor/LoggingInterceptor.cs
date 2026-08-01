using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EventForge.LoggingDBInterceptor;

/// <summary>
/// Interceptor для логирования SQL-запросов, выполняемых через Entity Framework Core
/// </summary>
public class LoggingInterceptor(ILogger<LoggingInterceptor> logger) : DbCommandInterceptor
{
    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        if (eventData.Duration.TotalMilliseconds > 500) 
            LogCommand("Медленный запрос", command, eventData);

        return base.ReaderExecuted(command, eventData, result);
    }

    // Асинхронная версия (для ToListAsync, FirstOrDefaultAsync и т.д.)
    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken ct = default)
    {

        if (eventData.Duration.TotalMilliseconds > 500) 
            LogCommand("Медленный запрос", command, eventData);

        return await base.ReaderExecutedAsync(command, eventData, result, ct);
    }

    private void LogCommand(string message, DbCommand command, CommandExecutedEventData eventData) =>
        // Кастомная логика с маскировкой данных
        logger.LogInformation("SQL ({Duration}ms): {Sql}. Сообщение: {message}",
            eventData.Duration.TotalMilliseconds,
            command.CommandText,
            message);
}

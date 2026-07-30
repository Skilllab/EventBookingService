using EventForge.Events.Infrastructure.Entities;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Events.Infrastructure.Context;

/// <summary>
/// DbContext сервиса событий
/// </summary>
public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Таблица событий
    /// </summary>
    public DbSet<EventEntity> Events => Set<EventEntity>();

    /// <summary>
    /// Таблица обработанных сообщений
    /// </summary>
    public DbSet<ProcessedMessageEntity> ProcessedMessages => Set<ProcessedMessageEntity>();

    /// <summary>
    /// Таблица outbox-сообщений
    /// </summary>
    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Events");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventsDbContext).Assembly);
    }
}

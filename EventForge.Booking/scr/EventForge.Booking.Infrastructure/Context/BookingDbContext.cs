using EventForge.Booking.Infrastructure.Entities;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Booking.Infrastructure.Context;

/// <summary>
/// DbContext сервиса бронирования
/// </summary>
public sealed class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Таблица бронирований
    /// </summary>
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();

    /// <summary>
    /// Таблица outbox-сообщений
    /// </summary>
    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    /// <summary>
    /// Таблица обработанных сообщений
    /// </summary>
    public DbSet<ProcessedMessageEntity> ProcessedMessages => Set<ProcessedMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Booking");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);
    }
}

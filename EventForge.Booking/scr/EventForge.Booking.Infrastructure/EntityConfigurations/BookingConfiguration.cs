using EventForge.Booking.Infrastructure.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventForge.Booking.Infrastructure.EntityConfigurations;

/// <summary>
/// Конфигурация EF для <see cref="BookingEntity"/>
/// </summary>
public class BookingConfiguration : IEntityTypeConfiguration<BookingEntity>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BookingEntity> builder)
    {
        builder.ToTable("Bookings");

        //Первичный ключ
        builder.HasKey(k => k.Id);
        //Отключаем генерацию в коде
        builder.Property(k => k.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UserId)
            .HasColumnName("user_id");

        builder.Property(p => p.ProcessedAt)
            .HasColumnName("processed_at");
    }
}

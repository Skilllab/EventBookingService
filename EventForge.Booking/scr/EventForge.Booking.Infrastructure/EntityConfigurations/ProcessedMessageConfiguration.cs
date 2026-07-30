using EventForge.Booking.Infrastructure.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventForge.Booking.Infrastructure.EntityConfigurations;

/// <summary>
/// EF-конфигурация таблицы обработанных сообщений
/// </summary>
public class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessageEntity>
{
    public void Configure(EntityTypeBuilder<ProcessedMessageEntity> builder)
    {
        builder.ToTable("ProcessedMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.MessageType)
            .HasColumnName("message_type")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired();
    }
}

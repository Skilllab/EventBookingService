using EventForge.Booking.Infrastructure.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventForge.Booking.Infrastructure.EntityConfigurations;

/// <summary>
/// EF-конфигурация таблицы outbox
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(x => x.Topic)
            .HasColumnName("topic")
            .IsRequired();

        builder.Property(x => x.MessageKey)
            .HasColumnName("message_key")
            .IsRequired();

        builder.Property(x => x.Payload)
            .HasColumnName("payload")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(x => x.Error)
            .HasColumnName("error");

        builder.Property(x => x.TraceParent)
            .HasColumnName("trace_parent");

        builder.Property(x => x.TraceState)
            .HasColumnName("trace_state");
    }
}

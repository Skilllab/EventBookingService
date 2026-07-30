using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Infrastructure.Context;
using EventForge.Booking.Infrastructure.Entities;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Booking.Infrastructure.Repositories;

/// <summary>
/// Репозиторий хранения обработанных сообщений
/// </summary>
public class ProcessedMessageRepository(IDbContextFactory<BookingDbContext> factory, TimeProvider timeProvider)
    : IProcessedMessageRepository
{
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);
        return await context.ProcessedMessages.AnyAsync(x => x.Id == id, ct);
    }

    public async Task AddAsync(Guid id, string messageType, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        await context.ProcessedMessages.AddAsync(new ProcessedMessageEntity
        {
            Id = id,
            MessageType = messageType,
            ProcessedAt = timeProvider.GetUtcNow().UtcDateTime
        }, ct);

        await context.SaveChangesAsync(ct);
    }
}

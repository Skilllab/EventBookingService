using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Infrastructure.Context;
using EventForge.Booking.Infrastructure.Mapping;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Booking.Infrastructure.Repositories;

/// <summary>
/// Репозиторий outbox-сообщений
/// </summary>
public class OutboxRepository(IDbContextFactory<BookingDbContext> factory, TimeProvider timeProvider) : IOutboxRepository
{
    public async Task<List<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        return await context.OutboxMessages
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .Select(x => x.ToDomain())
            .ToListAsync(ct);
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var entity = await context.OutboxMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null)
            return;

        entity.ProcessedAt = timeProvider.GetUtcNow().UtcDateTime;
        entity.Error = null;

        await context.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(Guid id, string error, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var entity = await context.OutboxMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null)
            return;

        entity.Error = error;
        await context.SaveChangesAsync(ct);
    }
}

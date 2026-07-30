using EventForge.Events.Application.Interfaces;
using EventForge.Events.Domain.Entities;
using EventForge.Events.Infrastructure.Context;
using EventForge.Events.Infrastructure.Mappers;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Events.Infrastructure.Repositories;

/// <summary>
/// Репозиторий outbox-сообщений.
/// </summary>
public class OutboxRepository(IDbContextFactory<EventsDbContext> factory, TimeProvider timeProvider) : IOutboxRepository
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

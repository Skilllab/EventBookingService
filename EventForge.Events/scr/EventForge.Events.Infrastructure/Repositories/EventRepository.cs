using EventForge.Events.Application.Interfaces;
using EventForge.Events.Domain.Entities;
using EventForge.Events.Infrastructure.Context;
using EventForge.Events.Infrastructure.Entities;
using EventForge.Events.Infrastructure.Mappers;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Events.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для событий
/// </summary>
/// <param name="factory"></param>
public class EventRepository(IDbContextFactory<EventsDbContext> factory) : IEventRepository
{
    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);
        var entity = await context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        return entity?.ToDomain();
    }

    public async Task<PagedResult<Event>> GetPagedAsync(
        string? title,
        DateTime? startAt,
        DateTime? endAt,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        // Валидация
        if (page <= 0) throw new ArgumentException("Номер страницы пагинации не может быть менье 1");
        if (pageSize <= 0 || pageSize > 100) throw new ArgumentException("Размер страницы должен быть в пределах от 1 до 100");

        await using var context = await factory.CreateDbContextAsync(ct);

        var query = context.Events.AsNoTracking();

        query = CreateQuery(context, query, startAt, endAt, title);

        var totalCount = await query.CountAsync(ct);

        var entities = await query
            .OrderBy(e => e.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var domainEvents = entities
            .Select(e => e.ToDomain())
            .ToList();

        return new PagedResult<Event>(domainEvents, totalCount);
    }

    private IQueryable<EventEntity> CreateQuery(EventsDbContext? context, IQueryable<EventEntity> query, DateTime? startAt, DateTime? endAt, string? title)
    {
        if (!string.IsNullOrEmpty(title))
        {
            if (context != null && context.Database.IsNpgsql())//PostgreSQL
            {
                query = query.Where(e => EF.Functions.ILike(e.Title, $"%{title}%"));
            }
            else if (context != null && context.Database.IsSqlServer()) //MS SQL Server
            {
                // В SQL Server поиск по умолчанию регистронезависимый
                query = query.Where(e => e.Title.Contains(title));
            }
            else // Для остальных (SQLite и пр.)
            {
                query = query.Where(e => e.Title.ToLower().Contains(title.ToLower()));
            }
        }

        if (startAt.HasValue) query = query.Where(e => e.StartAt >= startAt);

        if (endAt.HasValue)
        {
            // Если время не указано (00:00:00), значит ищем до конца дня включительно
            query = endAt.Value.TimeOfDay == TimeSpan.Zero
                ? query.Where(e => e.EndAt.Date <= endAt.Value.Date)
                : query.Where(e => e.EndAt <= endAt.Value);
        }

        return query;
    }

    public async Task AddAsync(Event @event, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);
        var entity = @event.ToEntity();
        await context.AddAsync(entity, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Event @event, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);
        var entity = @event.ToEntity();
        context.Events.Update(entity);
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);
        var affected = await context.Events.Where(e => e.Id == id).ExecuteDeleteAsync(ct);
        return affected > 0;
    }

    public async Task SaveEventAndOutboxAsync(Event @event, OutboxMessage outboxMessage, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        context.Events.Update(@event.ToEntity());
        await context.OutboxMessages.AddAsync(outboxMessage.ToEntity(), ct);

        await context.SaveChangesAsync(ct);
    }

    public async Task AddOutboxAsync(OutboxMessage outboxMessage, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var outboxEntity = new OutboxMessageEntity
        {
            Id = outboxMessage.Id,
            Type = outboxMessage.Type,
            Topic = outboxMessage.Topic,
            MessageKey = outboxMessage.MessageKey,
            Payload = outboxMessage.Payload,
            CreatedAt = outboxMessage.CreatedAt,
            ProcessedAt = null,
            Error = null,
            // в AddOutboxAsync(...) добавить:
            TraceParent = outboxMessage.TraceParent,
            TraceState = outboxMessage.TraceState,
        };
        await context.OutboxMessages.AddAsync(outboxEntity, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<Top10PagedResult<Event>> GetTop10EventsAsync(CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var entities = await context.Events
            .AsNoTracking()
            .Where(e => e.AvailableSeats < e.TotalSeats)   // только с проданными местами
            .OrderByDescending(e => (double) (e.TotalSeats - e.AvailableSeats) / e.TotalSeats)
            .Take(10)
            .ToListAsync(ct);

        var domainEvents = entities
            .Select(e => e.ToDomain())
            .ToList();

        return new Top10PagedResult<Event>(domainEvents);

    }
}

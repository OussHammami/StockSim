using Microsoft.EntityFrameworkCore;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;

namespace StockSim.Infrastructure.Outbox;

/// <summary>
/// Outbox writer bound to ONE DbContext. Register per producing context.
/// </summary>
public sealed class EfOutboxWriter<TDbContext, TMarker> : IOutboxWriter<TMarker>
    where TDbContext : DbContext
{
    private readonly TDbContext _db;

    public EfOutboxWriter(TDbContext db) => _db = db;

    public async Task WriteAsync(IEnumerable<IntegrationEvent> events, CancellationToken ct = default)
    {
        int count = 0;
        foreach (var e in events)
        {
            await _db.Set<OutboxMessage>().AddAsync(new OutboxMessage
            {
                Type = e.Type,
                Source = e.Source,
                Subject = e.Subject,
                OccurredAt = e.OccurredAt,
                Data = e.Data,
                SchemaVersion = e.SchemaVersion,
                DedupeKey = e.DedupeKey
            }, ct);
            count++;
        }

        if (count > 0)
            await _db.SaveChangesAsync(ct);
    }
}

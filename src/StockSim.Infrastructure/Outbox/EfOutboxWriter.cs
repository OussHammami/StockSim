using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
using System.Diagnostics;

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
        var current = Activity.Current;
        var traceParent = current is not null && current.IdFormat == ActivityIdFormat.W3C
            ? current.Id
            : null;
        var traceState = current?.TraceStateString;
        var baggage = Baggage.Current.ToString();
        if (string.IsNullOrWhiteSpace(baggage))
            baggage = null;

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
                DedupeKey = e.DedupeKey,
                TraceParent = traceParent,
                TraceState = traceState,
                Baggage = baggage
            }, ct);
            count++;
        }

        if (count > 0)
            await _db.SaveChangesAsync(ct);
    }
}

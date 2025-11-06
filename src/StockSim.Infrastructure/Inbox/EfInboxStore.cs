using Microsoft.EntityFrameworkCore;
using StockSim.Application.Abstractions.Inbox;

namespace StockSim.Infrastructure.Inbox;

/// <summary>
/// Inbox store bound to ONE DbContext. Register per consuming context.
/// </summary>
public sealed class EfInboxStore<TDbContext, TMarker> : IInboxStore<TMarker>
    where TDbContext : DbContext
{
    private readonly TDbContext _db;

    public EfInboxStore(TDbContext db) => _db = db;

    public Task<bool> SeenAsync(string dedupeKey, CancellationToken ct = default) =>
        _db.Set<InboxMessage>().AnyAsync(x => x.DedupeKey == dedupeKey, ct);

    public async Task MarkAsync(string dedupeKey, CancellationToken ct = default)
    {
        _db.Set<InboxMessage>().Add(new InboxMessage { DedupeKey = dedupeKey });
        await _db.SaveChangesAsync(ct);
    }
}

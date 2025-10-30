using Microsoft.EntityFrameworkCore;
using StockSim.Application.Integration;
using StockSim.Infrastructure.Persistence.Portfolioing;
using StockSim.Infrastructure.Persistence.Trading;

namespace StockSim.Infrastructure.Inbox;

public sealed class EfInboxStore : IInboxStore
{
    private readonly TradingDbContext _trading;
    private readonly PortfolioDbContext _portfolio;

    public EfInboxStore(TradingDbContext trading, PortfolioDbContext portfolio)
    {
        _trading = trading;
        _portfolio = portfolio;
    }

    public async Task<bool> SeenAsync(string dedupeKey, CancellationToken ct = default)
    {
        // check both schemas
        var seenTrading = await _trading.Inbox.AnyAsync(x => x.DedupeKey == dedupeKey, ct);
        if (seenTrading) return true;
        var seenPortfolio = await _portfolio.Inbox.AnyAsync(x => x.DedupeKey == dedupeKey, ct);
        return seenPortfolio;
    }

    public async Task MarkAsync(string dedupeKey, CancellationToken ct = default)
    {
        // write to portfolio by default; adjust per-worker later
        _portfolio.Inbox.Add(new InboxMessage { DedupeKey = dedupeKey });
        await _portfolio.SaveChangesAsync(ct);
    }
}

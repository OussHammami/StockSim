using Microsoft.EntityFrameworkCore;
using StockSim.Application.Abstractions;
using StockSim.Domain.Entities;
using StockSim.Domain.Models;
using StockSim.Infrastructure.Persistence;
using StockSim.Infrastructure.Persistence.Entities;

namespace StockSim.Web.Services;

public sealed class PortfolioService(IDbContextFactory<ApplicationDbContext> factory) : IPortfolioService
{
    private const decimal DefaultStartingCash = 100_000m;

    public async Task ResetAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var pf = await db.Portfolios.FindAsync([userId], ct) ?? new PortfolioEntity { UserId = userId };
        pf.Cash = DefaultStartingCash;
        var pos = await db.Positions.Where(p => p.UserId == userId).ToListAsync(ct);
        var ord = await db.Orders.Where(p => p.UserId == userId).ToListAsync(ct);
        db.Positions.RemoveRange(pos);
        db.Portfolios.Update(pf);
        db.Orders.RemoveRange(ord);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> TryTradeAsync(string userId, string symbol, int qty, decimal price, CancellationToken ct, Action<string>? setError = null)
    {
        if (string.IsNullOrWhiteSpace(symbol) || qty == 0 || price <= 0) { setError?.Invoke("Invalid order."); return false; }


        await using var db = await factory.CreateDbContextAsync(ct);
        // Portfolio: load -> tracked; if missing -> Add
        var pf = await db.Portfolios.FindAsync([userId], ct);
        var pfNew = pf is null;
        if (pfNew)
        {
            pf = new PortfolioEntity { UserId = userId, Cash = DefaultStartingCash };
            db.Portfolios.Add(pf);
        }

        // Position: load -> tracked; if missing -> not tracked yet
        var pos = await db.Positions.FindAsync([userId, symbol], ct);
        var posExisted = pos is not null;
        pos ??= new PositionEntity { UserId = userId, Symbol = symbol, Quantity = 0, AvgPrice = 0 };

        if (qty > 0)
        {
            var cost = qty * price;
            if (pf!.Cash < cost) { setError?.Invoke("Insufficient cash."); return false; }

            var newQty = pos.Quantity + qty;
            pos.AvgPrice = newQty == 0 ? 0 : ((pos.AvgPrice * pos.Quantity) + cost) / newQty;
            pos.Quantity = newQty;
            pf.Cash -= cost;

            if (!posExisted) db.Positions.Add(pos); // existing is tracked; no Update needed
        }
        else
        {
            var sellQty = -qty;
            if (pos.Quantity < sellQty) { setError?.Invoke("Insufficient shares."); return false; }

            pos.Quantity -= sellQty;
            pf!.Cash += sellQty * price;

            if (pos.Quantity == 0)
            {
                if (posExisted) db.Positions.Remove(pos);
                // if it never existed, nothing to remove
            }
            else
            {
                if (!posExisted) db.Positions.Add(pos);
            }
        }

        // pf is tracked if loaded; if newly added we already called Add.
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PortfolioSnapshot> SnapshotAsync(string userId, IReadOnlyDictionary<string, Quote> lastQuotes, CancellationToken ct = default)
    {        
        await using var db = await factory.CreateDbContextAsync(ct);
        var pf = await db.Portfolios.AsNoTracking().Where(p => p.UserId == userId).FirstOrDefaultAsync(ct) ?? new PortfolioEntity { UserId = userId, Cash = DefaultStartingCash };
        var positions = await db.Positions.AsNoTracking().Where(p => p.UserId == userId).OrderBy(p => p.Symbol).ToListAsync(ct);

        decimal mv = 0, upnl = 0;
        var list = positions.Select(p => new Position { Symbol = p.Symbol, Quantity = p.Quantity, AvgPrice = p.AvgPrice }).ToList();
        foreach (var p in list)
            if (lastQuotes.TryGetValue(p.Symbol, out var q))
            { mv += p.Quantity * q.Price; upnl += p.Quantity * (q.Price - p.AvgPrice); }

        return new PortfolioSnapshot { Cash = pf.Cash, Positions = list, MarketValue = mv, UnrealizedPnl = upnl };
    }
}

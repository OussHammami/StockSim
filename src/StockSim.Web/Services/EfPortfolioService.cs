using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using StockSim.Shared.Models;
using StockSim.Web.Data;
using StockSim.Web.Data.Trading;

namespace StockSim.Web.Services;

public sealed class EfPortfolioService(ApplicationDbContext db, AuthenticationStateProvider auth) : IPortfolioServiceAsync
{
    private const decimal DefaultStartingCash = 100_000m;

    public async Task ResetAsync(CancellationToken ct = default)
    {
        var uid = await UserId(ct);
        var pf = await db.Portfolios.FindAsync([uid], ct) ?? new PortfolioEntity { UserId = uid };
        pf.Cash = DefaultStartingCash;
        var pos = await db.Positions.Where(p => p.UserId == uid).ToListAsync(ct);
        db.Positions.RemoveRange(pos);
        db.Portfolios.Update(pf);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> TryTradeAsync(string symbol, int qty, decimal price, CancellationToken ct, Action<string>? setError = null)
    {
        if (string.IsNullOrWhiteSpace(symbol) || qty == 0 || price <= 0) { setError?.Invoke("Invalid order."); return false; }

        var uid = await UserId(ct);

        // Portfolio: load -> tracked; if missing -> Add
        var pf = await db.Portfolios.FindAsync([uid], ct);
        var pfNew = pf is null;
        if (pfNew)
        {
            pf = new PortfolioEntity { UserId = uid, Cash = DefaultStartingCash };
            db.Portfolios.Add(pf);
        }

        // Position: load -> tracked; if missing -> not tracked yet
        var pos = await db.Positions.FindAsync([uid, symbol], ct);
        var posExisted = pos is not null;
        pos ??= new PositionEntity { UserId = uid, Symbol = symbol, Quantity = 0, AvgPrice = 0 };

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


    public async Task<PortfolioSnapshot> SnapshotAsync(IReadOnlyDictionary<string, Quote> lastQuotes, CancellationToken ct = default)
    {
        var uid = await UserId(ct);
        var pf = await db.Portfolios.FindAsync([uid], ct) ?? new PortfolioEntity { UserId = uid, Cash = DefaultStartingCash };
        var positions = await db.Positions.Where(p => p.UserId == uid).OrderBy(p => p.Symbol).ToListAsync(ct);

        decimal mv = 0, upnl = 0;
        var list = positions.Select(p => new Position { Symbol = p.Symbol, Quantity = p.Quantity, AvgPrice = p.AvgPrice }).ToList();
        foreach (var p in list)
            if (lastQuotes.TryGetValue(p.Symbol, out var q))
            { mv += p.Quantity * q.Price; upnl += p.Quantity * (q.Price - p.AvgPrice); }

        return new PortfolioSnapshot { Cash = pf.Cash, Positions = list, MarketValue = mv, UnrealizedPnl = upnl };
    }

    private async Task<string> UserId(CancellationToken ct)
    {
        var state = await auth.GetAuthenticationStateAsync();
        var uid = state.User.FindFirst("sub")?.Value
               ?? state.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (string.IsNullOrEmpty(uid)) throw new InvalidOperationException("User not authenticated.");
        return uid;
    }
}

public interface IPortfolioServiceAsync
{
    Task ResetAsync(CancellationToken ct = default);
    Task<bool> TryTradeAsync(string symbol, int qty, decimal price, CancellationToken ct, Action<string>? setError = null);
    Task<PortfolioSnapshot> SnapshotAsync(IReadOnlyDictionary<string, Quote> lastQuotes, CancellationToken ct = default);
}

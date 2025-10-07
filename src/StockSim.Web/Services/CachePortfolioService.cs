using StockSim.Shared.Models;

namespace StockSim.Web.Services;

public interface ICachePortfolioService
{
    void Reset(decimal startingCash = 100_000m);
    bool TryTrade(string symbol, int qty, decimal price, out string? error);
    PortfolioSnapshot Snapshot(IReadOnlyDictionary<string, Quote> lastQuotes);
}

public sealed class CachePortfolioService : ICachePortfolioService
{
    private decimal _cash;
    private readonly Dictionary<string, Position> _pos = new(StringComparer.OrdinalIgnoreCase);

    public CachePortfolioService() => Reset();

    public void Reset(decimal startingCash = 100_000m)
    {
        _cash = startingCash;
        _pos.Clear();
    }

    public bool TryTrade(string symbol, int qty, decimal price, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(symbol) || qty == 0 || price <= 0) { error = "Invalid order."; return false; }

        // Buy
        if (qty > 0)
        {
            var cost = qty * price;
            if (_cash < cost) { error = "Insufficient cash."; return false; }

            if (!_pos.TryGetValue(symbol, out var p))
                _pos[symbol] = p = new Position { Symbol = symbol, Quantity = 0, AvgPrice = 0 };

            var newQty = p.Quantity + qty;
            p.AvgPrice = newQty == 0 ? 0 : ((p.AvgPrice * p.Quantity) + cost) / newQty;
            p.Quantity = newQty;
            _cash -= cost;
            return true;
        }

        // Sell
        var sellQty = -qty;
        if (!_pos.TryGetValue(symbol, out var ps) || ps.Quantity < sellQty) { error = "Insufficient shares."; return false; }

        ps.Quantity -= sellQty;
        _cash += sellQty * price;
        if (ps.Quantity == 0) ps.AvgPrice = 0;
        return true;
    }

    public PortfolioSnapshot Snapshot(IReadOnlyDictionary<string, Quote> lastQuotes)
    {
        decimal mv = 0, upnl = 0;
        var list = _pos.Values.Where(p => p.Quantity != 0)
                              .OrderBy(p => p.Symbol).Select(p => new Position
                              { Symbol = p.Symbol, Quantity = p.Quantity, AvgPrice = p.AvgPrice }).ToList();

        foreach (var p in list)
        {
            if (lastQuotes.TryGetValue(p.Symbol, out var q))
            {
                mv += p.Quantity * q.Price;
                upnl += p.Quantity * (q.Price - p.AvgPrice);
            }
        }
        return new PortfolioSnapshot { Cash = _cash, Positions = list, MarketValue = mv, UnrealizedPnl = upnl };
    }
}

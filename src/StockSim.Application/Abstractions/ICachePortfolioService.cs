using StockSim.Domain.Models;

namespace StockSim.Application.Abstractions
{
    public interface ICachePortfolioService
    {
        void Reset(decimal startingCash = 100_000m);
        bool TryTrade(string symbol, int qty, decimal price, out string? error);
        PortfolioSnapshot Snapshot(IReadOnlyDictionary<string, Quote> lastQuotes);
    }
}

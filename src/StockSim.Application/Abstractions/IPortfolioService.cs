using StockSim.Domain.Models;

namespace StockSim.Application.Abstractions;

public interface IPortfolioService
{
    Task ResetAsync(string userId, CancellationToken ct = default);
    Task<bool> TryTradeAsync(string userId, string symbol, int qty, decimal price, CancellationToken ct,
                             Action<string>? setError = null);
    Task<PortfolioSnapshot> SnapshotAsync(string userId, IReadOnlyDictionary<string, Quote> lastQuotes,
                                          CancellationToken ct = default);
}

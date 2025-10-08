using StockSim.Domain.Entities;

namespace StockSim.Domain.Models;

public sealed class PortfolioSnapshot
{
    public decimal Cash { get; init; }
    public IReadOnlyList<Position> Positions { get; init; } = Array.Empty<Position>();
    public decimal MarketValue { get; init; }
    public decimal UnrealizedPnl { get; init; }
    public decimal Equity => Cash + MarketValue;
}

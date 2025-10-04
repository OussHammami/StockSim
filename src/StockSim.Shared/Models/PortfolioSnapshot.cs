namespace StockSim.Shared.Models;

public sealed class PortfolioSnapshot
{
    public decimal Cash { get; init; }
    public IReadOnlyList<Position> Positions { get; init; } = Array.Empty<Position>();
    public decimal MarketValue { get; init; }        // sum(qty * last)
    public decimal UnrealizedPnl { get; init; }      // sum(qty * (last - avg))
    public decimal Equity => Cash + MarketValue;
}

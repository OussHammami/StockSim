namespace StockSim.Contracts.Portfolio;

public sealed record FillAppliedV1(
    string PortfolioId,
    string OrderId,
    string Side,     // "Buy" | "Sell"
    string Symbol,
    decimal Quantity,
    decimal Price,
    decimal CashDelta,
    decimal NewPositionQty,
    decimal NewAvgCost);

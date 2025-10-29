namespace StockSim.Contracts.Portfolio;

public sealed record ReservationReleasedV1(
    string PortfolioId,
    string OrderId,
    decimal? Funds,
    string? Symbol,
    decimal? Shares,
    string Reason);

namespace StockSim.Contracts.Portfolio;

public sealed record SharesReservedV1(
    string PortfolioId,
    string OrderId,
    string Symbol,
    decimal Quantity);

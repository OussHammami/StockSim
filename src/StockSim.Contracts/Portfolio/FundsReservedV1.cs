namespace StockSim.Contracts.Portfolio;

public sealed record FundsReservedV1(
    string PortfolioId,
    string OrderId,
    decimal Amount);

namespace StockSim.Contracts.Trading;

public sealed record OrderRejectedV1(
    string OrderId,
    string Reason);

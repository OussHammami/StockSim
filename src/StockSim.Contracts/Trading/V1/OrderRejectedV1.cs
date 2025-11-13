namespace StockSim.Contracts.Trading.V1;

public sealed record OrderRejectedV1(
    string OrderId,
    string Reason);

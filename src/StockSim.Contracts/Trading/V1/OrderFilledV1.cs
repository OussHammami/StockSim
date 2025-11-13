namespace StockSim.Contracts.Trading.V1;

public sealed record OrderFilledV1(
    string OrderId,
    decimal TotalFilledQuantity,
    decimal AverageFillPrice);

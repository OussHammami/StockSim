namespace StockSim.Contracts.Trading;

public sealed record OrderFilledV1(
    string OrderId,
    decimal TotalFilledQuantity,
    decimal AverageFillPrice);

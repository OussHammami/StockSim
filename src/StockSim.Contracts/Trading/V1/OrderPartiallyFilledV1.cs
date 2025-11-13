namespace StockSim.Contracts.Trading.V1;

public sealed record OrderPartiallyFilledV1(
    string OrderId,
    decimal FillQuantity,
    decimal FillPrice,
    decimal CumFilledQuantity);

namespace StockSim.Contracts.Trading;

public sealed record OrderPartiallyFilledV1(
    string OrderId,
    decimal FillQuantity,
    decimal FillPrice,
    decimal CumFilledQuantity);

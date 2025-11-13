namespace StockSim.Contracts.Trading.V2;

public sealed record OrderPartiallyFilledV2(
    string OrderId,
    string UserId,
    string Symbol,
    string Side,                 // "Buy" | "Sell"
    decimal FillQuantity,
    decimal FillPrice,
    decimal CumFilledQuantity,
    DateTimeOffset OccurredAt);
namespace StockSim.Contracts.Trading;

public sealed record OrderAcceptedV1(
    string OrderId,
    string UserId,
    string Symbol,
    string Side,    // "Buy" | "Sell"
    string Type,    // "Market" | "Limit"
    decimal Quantity,
    decimal? LimitPrice,
    DateTimeOffset OccurredAt);

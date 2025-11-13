namespace StockSim.Contracts.Trading.V2
{
    public sealed record OrderFilledV2(
    string OrderId,
    string UserId,
    string Symbol,
    string Side,                 // "Buy" | "Sell"
    decimal TotalFilledQuantity,
    decimal AverageFillPrice,
    DateTimeOffset OccurredAt);
}

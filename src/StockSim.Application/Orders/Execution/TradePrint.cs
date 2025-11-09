namespace StockSim.Application.Orders.Execution;

public enum TradeAggressor
{
    Unknown = 0,
    Buyer = 1, // trade likely executed at ask (buy aggression)
    Seller = 2 // trade likely executed at bid (sell aggression)
}

public sealed record TradePrint(
    string Symbol,
    decimal Price,
    decimal Quantity,
    TradeAggressor Aggressor,
    DateTimeOffset Timestamp);
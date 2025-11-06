namespace StockSim.Domain.MarketFeed;

public record Quote(string Symbol, decimal Bid, decimal Ask, decimal? Last, DateTimeOffset Ts);

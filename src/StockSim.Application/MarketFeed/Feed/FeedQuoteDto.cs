namespace StockSim.Application.MarketData.Feed;

public sealed record FeedQuoteDto(
    string Ticker,          // may have lower/upper/mixed case
    decimal? Bid,
    decimal? Ask,
    decimal? Last,
    DateTimeOffset? TsUtc);

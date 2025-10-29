namespace StockSim.Application.MarketData.Feed;

public interface IMarketDataFeed
{
    IAsyncEnumerable<FeedQuoteDto> StreamAsync(CancellationToken ct = default);
}

using System.Runtime.CompilerServices;

namespace StockSim.Application.MarketData.Feed;

public sealed class FakeMarketDataFeed : IMarketDataFeed
{
    private readonly string[] _tickers = new[] { "AAPL", "MSFT", "TSLA", "NVDA" };
    private readonly Random _rng = new();

    public async IAsyncEnumerable<FeedQuoteDto> StreamAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        var px = new Dictionary<string, decimal> { ["AAPL"] = 190, ["MSFT"] = 420, ["TSLA"] = 220, ["NVDA"] = 830 };

        while (!ct.IsCancellationRequested)
        {
            var t = _tickers[_rng.Next(_tickers.Length)];
            var last = px[t] = Math.Max(1, px[t] + (decimal)(_rng.NextDouble() - 0.5) * 2m);
            var bid = last - 0.05m;
            var ask = last + 0.05m;

            yield return new FeedQuoteDto(t, bid, ask, last, DateTimeOffset.UtcNow);
            await Task.Delay(250, ct);
        }
    }
}

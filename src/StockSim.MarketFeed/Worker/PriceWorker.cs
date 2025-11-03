using Microsoft.AspNetCore.SignalR;
using StockSim.Domain.MarketFeed;
using System.Collections.Concurrent;
using StockSim.MarketFeed.Hubs;

public sealed class PriceWorker(ConcurrentDictionary<string, decimal> prices,
                                string[] symbols,
                                IHubContext<QuoteHub> hub) : BackgroundService
{
    private readonly Random _rng = new();

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        foreach (var s in symbols)
            prices[s] = (decimal)_rng.NextDouble() * 100m;

        while (!token.IsCancellationRequested)
        {
            foreach (var s in symbols)
            {
                var last = prices[s] = Math.Max(1, prices[s] + (decimal)(_rng.NextDouble() - 0.5) * 2m);
                var bid = last - 0.05m;
                var ask = last + 0.05m;
                var q = new Quote(s, bid, ask, last, DateTimeOffset.UtcNow);
                prices[s] = last;
                await hub.Clients.All.SendAsync("quote", q, token);
            }
            await Task.Delay(5000, token);
        }
    }
}

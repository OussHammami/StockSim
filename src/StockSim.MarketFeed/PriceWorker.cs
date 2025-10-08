using Microsoft.AspNetCore.SignalR;
using StockSim.MarketFeed;
using StockSim.Domain.Models;
using System.Collections.Concurrent;

public sealed class PriceWorker(ConcurrentDictionary<string, Quote> prices,
                                string[] symbols,
                                IHubContext<QuoteHub> hub) : BackgroundService
{
    private readonly Random _rng = new();

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        foreach (var s in symbols)
            prices[s] = new Quote(s, 100m + (decimal)_rng.NextDouble() * 100m, 0m, DateTimeOffset.UtcNow);

        while (!token.IsCancellationRequested)
        {
            foreach (var s in symbols)
            {
                var old = prices[s];
                var pct = ((decimal)_rng.NextDouble() - 0.5m) * 0.02m;
                var p = Math.Max(1m, old.Price * (1m + pct));
                var q = new Quote(s, decimal.Round(p, 2), decimal.Round(p - old.Price, 2), DateTimeOffset.UtcNow);
                prices[s] = q;
                await hub.Clients.All.SendAsync("quote", q, token);
            }
            await Task.Delay(5000, token);
        }
    }
}

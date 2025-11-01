using Microsoft.AspNetCore.SignalR;
using StockSim.Application.MarketData.Feed;
using StockSim.Web.Hubs;

namespace StockSim.Web.Services;

public sealed class MarketDataStreamer : BackgroundService
{
    private readonly IMarketDataFeed _feed;
    private readonly IHubContext<QuotesHub> _hub;
    private readonly ILogger<MarketDataStreamer> _log;

    public MarketDataStreamer(IMarketDataFeed feed, IHubContext<QuotesHub> hub, ILogger<MarketDataStreamer> log)
    {
        _feed = feed;
        _hub = hub;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var dto in _feed.StreamAsync(stoppingToken))
        {
            var q = QuoteAcl.Map(dto);
            if (q is null) continue;

            var payload = new
            {
                Symbol = q.Symbol.Value,
                Bid = q.Bid.Value,
                Ask = q.Ask.Value,
                Last = q.Last?.Value,
                Ts = q.Timestamp
            };

            // broadcast to group per symbol and to all
            await _hub.Clients.Group(q.Symbol.Value).SendAsync("quote", payload, stoppingToken);
            await _hub.Clients.All.SendAsync("quote", payload, stoppingToken);
        }
    }
}

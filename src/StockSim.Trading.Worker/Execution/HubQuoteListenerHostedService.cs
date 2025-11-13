using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockSim.Application.Options;
using StockSim.Application.Orders.Execution;

namespace StockSim.Trading.Worker.Execution;

// Connects to the MarketFeed SignalR hub and forwards incoming quotes to HubQuoteSnapshotProvider.
public sealed class HubQuoteListenerHostedService : BackgroundService
{
    private readonly ILogger<HubQuoteListenerHostedService> _log;
    private readonly IConfiguration _cfg;
    private readonly HubQuoteSnapshotProvider _provider;
    private readonly IOptions<MarketFeedOptions> _feed;
    private HubConnection? _conn;

    public HubQuoteListenerHostedService(
        ILogger<HubQuoteListenerHostedService> log,
        IConfiguration cfg,
        HubQuoteSnapshotProvider provider,
        IOptions<MarketFeedOptions> feed)
    {
        _log = log;
        _cfg = cfg;
        _provider = provider;
        _feed = feed;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var explicitUrl = _cfg.GetValue<string>("QUOTES__HUB_URL");
        var optHubUrl = _feed.Value.HubUrl;
        var hubUrl = !string.IsNullOrWhiteSpace(explicitUrl)
            ? explicitUrl
            : (!string.IsNullOrWhiteSpace(optHubUrl) ? optHubUrl : "http://marketfeed:8081/hubs/quotes");

        if (string.IsNullOrWhiteSpace(hubUrl))
        {
            _log.LogWarning("Quotes hub URL not configured (QUOTES__HUB_URL or MarketFeed__BaseUrl). Skipping hub connection.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _conn = new HubConnectionBuilder()
                    .WithUrl(hubUrl!)
                    .WithAutomaticReconnect()
                    .Build();

                // The exact server method name may differ; wire multiple common names defensively.
                _conn.On<QuoteMsg>("quote", async q => await _provider.PublishAsync(q));
                _conn.On<QuoteMsg>("Quotes", async q => await _provider.PublishAsync(q));
                _conn.On("Quote", async (string symbol, decimal bid, decimal ask, decimal? last, DateTimeOffset ts) =>
                {
                    await _provider.PublishAsync(new QuoteMsg(symbol, bid, ask, last, ts));
                });

                await _conn.StartAsync(stoppingToken);
                _log.LogInformation("Connected to quotes hub at {Url}.", hubUrl);

                // Wait until canceled or disconnected
                while (_conn.State == HubConnectionState.Connected && !stoppingToken.IsCancellationRequested)
                    await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Quotes hub connection loop error. Reconnecting shortly...");
            }

            // Backoff before reconnect
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_conn is not null)
        {
            try { await _conn.DisposeAsync(); } catch { }
        }
        await base.StopAsync(cancellationToken);
    }
}
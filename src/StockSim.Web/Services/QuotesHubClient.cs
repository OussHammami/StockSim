using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockSim.Application.Options;

namespace StockSim.Web.Services;

public sealed record QuoteMsg(string Symbol, decimal Bid, decimal Ask, decimal? Last, DateTimeOffset Ts);

public sealed class QuotesHubClient : IAsyncDisposable
{
    private readonly ILogger<QuotesHubClient> _log;
    private readonly MarketFeedOptions _opt;
    private HubConnection? _conn;

    public QuotesHubClient(IOptions<MarketFeedOptions> opt, ILogger<QuotesHubClient> log)
    {
        _opt = opt.Value;
        _log = log;
        if (string.IsNullOrWhiteSpace(_opt.BaseUrl))
            throw new InvalidOperationException("MarketFeed:BaseUrl is not configured.");
    }

    public event Action<QuoteMsg>? OnQuote;

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_conn is { State: HubConnectionState.Connected })
            return;

        var url = _opt.HubUrl;
        _conn = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build();

        _conn.On<QuoteMsg>("quote", msg =>
        {
            try { OnQuote?.Invoke(msg); }
            catch (Exception ex) { _log.LogWarning(ex, "OnQuote handler threw."); }
        });

        await _conn.StartAsync(ct);
        _log.LogInformation("Connected to MarketFeed hub: {Url}", url);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (_conn is null) return;
        try
        {
            await _conn.StopAsync(ct);
            await _conn.DisposeAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Error stopping quotes hub.");
        }
        finally
        {
            _conn = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_conn is not null)
        {
            try { await _conn.DisposeAsync(); } catch { }
            _conn = null;
        }
        await Task.CompletedTask;
    }
}

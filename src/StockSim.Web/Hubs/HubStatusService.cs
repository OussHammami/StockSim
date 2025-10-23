using Microsoft.AspNetCore.SignalR.Client;
namespace StockSim.Web.Hubs;
public sealed class HubStatusService : IAsyncDisposable
{
    private readonly IConfiguration _cfg;
    private HubConnection? _conn;
    private bool _connected;
    public bool Connected => _connected;
    public event Action<bool>? Changed;

    public HubStatusService(IConfiguration cfg) => _cfg = cfg;

    public async Task EnsureAsync(CancellationToken ct = default)
    {
        if (_conn != null) return;

        var baseUrl = _cfg["MarketFeed:BaseUrl"] ?? "http://localhost:8081";
        var url = new Uri($"{baseUrl.TrimEnd('/')}/hubs/quotes");

        _conn = new HubConnectionBuilder()
            .WithUrl(url)                 // cross-origin to MarketFeed
            .WithAutomaticReconnect()
            .Build();

        _conn.Reconnecting += _ => { Set(false); return Task.CompletedTask; };
        _conn.Reconnected  += _ => { Set(true);  return Task.CompletedTask; };
        _conn.Closed       += _ => { Set(false); return Task.CompletedTask; };

        await _conn.StartAsync(ct);
        Set(true);
    }

    private void Set(bool v) { _connected = v; Changed?.Invoke(v); }
    public async ValueTask DisposeAsync() { if (_conn != null) await _conn.DisposeAsync(); }
}

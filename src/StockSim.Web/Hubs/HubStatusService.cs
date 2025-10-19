using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace StockSim.Web.Services;

public sealed class HubStatusService : IAsyncDisposable
{
    private readonly NavigationManager _nav;
    private readonly ILogger<HubStatusService> _log;
    private HubConnection? _conn;

    public bool Connected { get; private set; }
    public event Action<bool>? Changed;

    public HubStatusService(NavigationManager nav, ILogger<HubStatusService> log)
    {
        _nav = nav;
        _log = log;
    }

    public async Task EnsureConnectedAsync(CancellationToken ct = default)
    {
        if (_conn != null) return;

        var hubUri = _nav.ToAbsoluteUri("/hubs/quotes"); // adjust path if different
        _conn = new HubConnectionBuilder()
            .WithUrl(hubUri)
            .WithAutomaticReconnect()
            .Build();

        _conn.Reconnecting += error =>
        {
            Set(false);
            return Task.CompletedTask;
        };
        _conn.Reconnected += id =>
        {
            Set(true);
            return Task.CompletedTask;
        };
        _conn.Closed += error =>
        {
            Set(false);
            return Task.CompletedTask;
        };

        await _conn.StartAsync(ct);
        Set(true);
    }

    private void Set(bool value)
    {
        if (Connected == value) return;
        Connected = value;
        try { Changed?.Invoke(value); } catch (Exception e) { _log.LogWarning(e, "Hub status change handler failed"); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_conn is null) return;
        try { await _conn.DisposeAsync(); } catch { /* ignore */ }
        _conn = null;
        Connected = false;
    }
}

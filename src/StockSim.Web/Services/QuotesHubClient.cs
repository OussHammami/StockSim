using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace StockSim.Web.Services;

public sealed class QuotesHubClient : IAsyncDisposable
{
    private readonly NavigationManager _nav;
    public HubConnection? Connection { get; private set; }
    public event Action<QuoteMsg>? OnQuote;

    public sealed record QuoteMsg(string Symbol, decimal Bid, decimal Ask, decimal? Last, DateTimeOffset Ts);

    public QuotesHubClient(NavigationManager nav) => _nav = nav;

    public async Task StartAsync(CancellationToken ct = default)
    {
        Connection ??= new HubConnectionBuilder()
            .WithUrl(_nav.ToAbsoluteUri("/hubs/quotes"))
            .WithAutomaticReconnect()
            .Build();

        Connection.On<QuoteMsg>("quote", m => OnQuote?.Invoke(m));
        if (Connection.State != HubConnectionState.Connected)
            await Connection.StartAsync(ct);
    }

    public Task SubscribeAsync(string symbol, CancellationToken ct = default) =>
        Connection is null ? Task.CompletedTask : Connection.InvokeAsync("Subscribe", symbol, ct);

    public async ValueTask DisposeAsync()
    {
        if (Connection is not null) await Connection.DisposeAsync();
    }
}

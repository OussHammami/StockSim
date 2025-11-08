using StockSim.Application.Orders.Execution;
using System.Collections.Concurrent;

public sealed class HubQuoteSnapshotProvider : IQuoteSnapshotProvider, IQuoteStream
{
    private readonly ConcurrentDictionary<string, QuoteSnapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Func<QuoteMsg, Task>> _subs = new();

    public QuoteSnapshot? Get(string symbol) =>
        _snapshots.TryGetValue(symbol, out var s) ? s : null;

    public IDisposable Subscribe(Func<QuoteMsg, Task> onQuote)
    {
        _subs.Add(onQuote);
        return new Subscription(_subs, onQuote);
    }

    // Called by SignalR hub handler
    public async Task OnQuoteAsync(QuoteMsg msg)
    {
        _snapshots[msg.Symbol] = new QuoteSnapshot(msg.Symbol, msg.Bid, msg.Ask, msg.Last, msg.Ts);
        foreach (var s in _subs)
            await s(msg);
    }

    private sealed class Subscription : IDisposable
    {
        private readonly List<Func<QuoteMsg, Task>> _list;
        private readonly Func<QuoteMsg, Task> _fn;
        public Subscription(List<Func<QuoteMsg, Task>> list, Func<QuoteMsg, Task> fn) { _list = list; _fn = fn; }
        public void Dispose() => _list.Remove(_fn);
    }
}
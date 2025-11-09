using System.Collections.Concurrent;
using StockSim.Application.Orders.Execution;

namespace StockSim.Trading.Worker.Execution;

// Provides latest quote snapshots and allows subscription to live quote messages.
// TapeDealerHostedService depends on IQuoteSnapshotProvider to price prints.
// Other services may subscribe via IQuoteStream if needed.
public sealed class HubQuoteSnapshotProvider : IQuoteSnapshotProvider, IQuoteStream
{
    private readonly ConcurrentDictionary<string, QuoteSnapshot> _snapshots =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly List<Func<QuoteMsg, Task>> _subs = new();
    private readonly object _subsLock = new();

    public QuoteSnapshot? Get(string symbol)
    {
        return _snapshots.TryGetValue(symbol, out var snap) ? snap : null;
    }

    // Publish is called by a listener (e.g., HubQuoteListenerHostedService) when a new quote arrives.
    public async Task PublishAsync(QuoteMsg msg)
    {
        // Update snapshot
        _snapshots[msg.Symbol] = new QuoteSnapshot(
            msg.Symbol,
            msg.Bid,
            msg.Ask,
            msg.Last,
            msg.Ts
        );

        // Notify subscribers
        Func<QuoteMsg, Task>[] subsCopy;
        lock (_subsLock) subsCopy = _subs.ToArray();

        foreach (var cb in subsCopy)
        {
            try { await cb(msg); } catch { /* swallow to avoid blocking */ }
        }
    }

    public IDisposable Subscribe(Func<QuoteMsg, Task> onQuote)
    {
        lock (_subsLock) _subs.Add(onQuote);
        return new Sub(_subs, _subsLock, onQuote);
    }

    private sealed class Sub : IDisposable
    {
        private readonly List<Func<QuoteMsg, Task>> _list;
        private readonly object _lock;
        private readonly Func<QuoteMsg, Task> _fn;
        public Sub(List<Func<QuoteMsg, Task>> list, object l, Func<QuoteMsg, Task> fn)
        {
            _list = list; _lock = l; _fn = fn;
        }

        public void Dispose()
        {
            lock (_lock) _list.Remove(_fn);
        }
    }
}
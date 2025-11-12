using System.Collections.Concurrent;

namespace StockSim.Application.Orders.Execution
{
    public sealed class SymbolLocks
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);
        public SemaphoreSlim For(string symbol) => _locks.GetOrAdd(symbol, _ => new SemaphoreSlim(1, 1));
    }
}

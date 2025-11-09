using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;
using System.Collections.Concurrent;

namespace StockSim.Application.Orders.Execution;

/// <summary>
/// Simple per-symbol order book. Maintains resting limit orders only.
/// Market orders are never stored here.
/// For production, move to a dedicated service or persistent structure.
/// </summary>
public sealed class OrderBook
{
    // For each symbol: bids (sorted high→low), asks (sorted low→high)
    private readonly ConcurrentDictionary<string, SortedSet<RestingOrder>> _bids = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SortedSet<RestingOrder>> _asks = new(StringComparer.OrdinalIgnoreCase);

    private readonly object _lock = new();

    public void Upsert(Order order)
    {
        if (order.Type != OrderType.Limit) return;
        if (order.State is not OrderState.Accepted and not OrderState.PartiallyFilled) return;
        if (order.RemainingQuantity <= 0m) return;

        var book = order.Side == OrderSide.Buy ? _bids : _asks;
        var set = book.GetOrAdd(order.Symbol.Value, _ => CreateSet(order.Side));
        lock (_lock)
        {
            // Remove existing entry if any (by Id) then add updated
            var existing = set.FirstOrDefault(r => r.Id == order.Id);
            if (existing is not null) set.Remove(existing);
            set.Add(new RestingOrder(order.Id, order.Symbol.Value, order.Side, order.LimitPrice!.Value, order.RemainingQuantity, order.CreatedAt));
        }
    }

    public void Remove(Order order)
    {
        if (order.Type != OrderType.Limit) return;
        var book = order.Side == OrderSide.Buy ? _bids : _asks;
        if (!book.TryGetValue(order.Symbol.Value, out var set)) return;
        lock (_lock)
        {
            var existing = set.FirstOrDefault(r => r.Id == order.Id);
            if (existing is not null) set.Remove(existing);
        }
    }

    public CrossingResult Cross(Symbol symbol, decimal maxLiquidity)
    {
        // Attempt internal crossing: highest bid >= lowest ask
        if (!_bids.TryGetValue(symbol.Value, out var bids) ||
            !_asks.TryGetValue(symbol.Value, out var asks) ||
            bids.Count == 0 || asks.Count == 0) return CrossingResult.Empty;

        lock (_lock)
        {
            var bestBid = bids.First();     // highest price due to comparer
            var bestAsk = asks.First();     // lowest price due to comparer

            if (bestBid.Price < bestAsk.Price) return CrossingResult.Empty;

            var qty = Math.Min(bestBid.Remaining, Math.Min(bestAsk.Remaining, maxLiquidity));
            if (qty <= 0m) return CrossingResult.Empty;

            // create proposed internal trade at mid or choose price (e.g., ask or bid). We'll pick midpoint.
            var price = (bestBid.Price + bestAsk.Price) / 2m;

            return new CrossingResult(
                new InternalCross(
                    symbol.Value,
                    bestBid.Id,
                    bestAsk.Id,
                    qty,
                    price));
        }
    }

    private static SortedSet<RestingOrder> CreateSet(OrderSide side) =>
        side == OrderSide.Buy
            ? new SortedSet<RestingOrder>(new BidComparer())
            : new SortedSet<RestingOrder>(new AskComparer());

    // Lightweight DTO
    public sealed record RestingOrder(
        OrderId Id,
        string Symbol,
        OrderSide Side,
        decimal Price,
        decimal Remaining,
        DateTime CreatedAt);

    public sealed record InternalCross(
        string Symbol,
        OrderId BidOrderId,
        OrderId AskOrderId,
        decimal Quantity,
        decimal Price);

    public sealed record CrossingResult(InternalCross? Cross)
    {
        public static CrossingResult Empty => new ((InternalCross?) null);
        public bool HasCross => Cross is not null;
    }

    private sealed class BidComparer : IComparer<RestingOrder>
    {
        public int Compare(RestingOrder? x, RestingOrder? y)
        {
            if (x == null || y == null) return 0;
            var priceCmp = -x.Price.CompareTo(y.Price); // high→low
            return priceCmp != 0 ? priceCmp : x.CreatedAt.CompareTo(y.CreatedAt);
        }
    }

    private sealed class AskComparer : IComparer<RestingOrder>
    {
        public int Compare(RestingOrder? x, RestingOrder? y)
        {
            if (x == null || y == null) return 0;
            var priceCmp = x.Price.CompareTo(y.Price); // low→high
            return priceCmp != 0 ? priceCmp : x.CreatedAt.CompareTo(y.CreatedAt);
        }
    }
}
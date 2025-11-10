using StockSim.Application.Abstractions.Events;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
using StockSim.Application.Orders;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Orders.Execution;

/// <summary>
/// Applies a trade print to currently open orders using price–time priority.
/// - If both sides (buy and sell) exist and satisfy the price, pairs them and fills both at the tape price.
/// - If only one side exists, fills that side against an implicit dealer (no portfolio needed).
/// </summary>
public sealed class TradePrintExecutor
{
    private readonly IOrderRepository _orders;
    private readonly IEventDispatcher _dispatcher;
    private readonly IIntegrationEventMapper _mapper;
    private readonly IOutboxWriter<ITradingOutboxContext> _outbox;
    private readonly SymbolLocks _locks = new();

    public TradePrintExecutor(
        IOrderRepository orders,
        IEventDispatcher dispatcher,
        IIntegrationEventMapper mapper,
        IOutboxWriter<ITradingOutboxContext> outbox)
    {
        _orders = orders;
        _dispatcher = dispatcher;
        _mapper = mapper;
        _outbox = outbox;
    }

    public async Task ExecuteAsync(TradePrint print, CancellationToken ct = default)
    {
        var gate = _locks.For(print.Symbol);
        await gate.WaitAsync(ct);
        try
        {
            var symbol = Symbol.From(print.Symbol);
            var tapePrice = Price.From(print.Price);
            var sizeRemaining = print.Quantity;

            // Load all open orders for symbol once and filter in-memory.
            // For scale, add repository methods to pre-filter by side & price.
            var allOpen = await _orders.GetOpenBySymbolAsync(symbol, ct).ConfigureAwait(false);

            // Eligible candidates by price constraints
            var buys = allOpen
                .Where(o => o.Side == OrderSide.Buy &&
                            (o.Type == OrderType.Market || (o.LimitPrice?.Value ?? decimal.MinValue) >= tapePrice.Value))
                .OrderByDescending(o => o.LimitPrice?.Value ?? decimal.MaxValue) // price priority: higher first
                .ThenBy(o => o.CreatedAt)
                .ToList();

            var sells = allOpen
                .Where(o => o.Side == OrderSide.Sell &&
                            (o.Type == OrderType.Market || (o.LimitPrice?.Value ?? decimal.MaxValue) <= tapePrice.Value))
                .OrderBy(o => o.LimitPrice?.Value ?? decimal.MinValue) // price priority: lower first
                .ThenBy(o => o.CreatedAt)
                .ToList();

            if (buys.Count == 0 && sells.Count == 0) return;

            // Pair both sides first if present
            if (buys.Count > 0 && sells.Count > 0)
            {
                int bi = 0, si = 0;
                while (sizeRemaining > 0 && bi < buys.Count && si < sells.Count)
                {
                    var b = buys[bi];
                    var s = sells[si];

                    var qty = Math.Min(sizeRemaining, Math.Min(b.RemainingQuantity, s.RemainingQuantity));
                    if (qty <= 0) break;

                    ApplyFill(b, qty, tapePrice);
                    ApplyFill(s, qty, tapePrice);

                    sizeRemaining -= qty;

                    if (b.RemainingQuantity <= 0) bi++;
                    if (s.RemainingQuantity <= 0) si++;
                }
            }

            // If tape size remains, fill one-sided against implicit dealer
            if (sizeRemaining > 0)
            {
                if (buys.Count > 0 && sells.Count == 0)
                {
                    foreach (var b in buys)
                    {
                        if (sizeRemaining <= 0) break;
                        var qty = Math.Min(sizeRemaining, b.RemainingQuantity);
                        if (qty <= 0) continue;
                        ApplyFill(b, qty, tapePrice);
                        sizeRemaining -= qty;
                    }
                }
                else if (sells.Count > 0 && buys.Count == 0)
                {
                    foreach (var s in sells)
                    {
                        if (sizeRemaining <= 0) break;
                        var qty = Math.Min(sizeRemaining, s.RemainingQuantity);
                        if (qty <= 0) continue;
                        ApplyFill(s, qty, tapePrice);
                        sizeRemaining -= qty;
                    }
                }
            }

            // Dispatch and persist
            var domainEvents = allOpen.SelectMany(o => o.DomainEvents).ToArray();
            if (domainEvents.Length == 0) return;

            await _dispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

            var integrationEvents = _mapper.Map(domainEvents).ToArray();
            foreach (var o in allOpen) o.ClearDomainEvents();

            await _outbox.WriteAsync(integrationEvents, ct).ConfigureAwait(false);
            await _orders.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
        
    }

    private static void ApplyFill(Domain.Orders.Order o, decimal qty, Price price)
    {
        // Respect simple IOC/FOK semantics inline (optional):
        if (o.TimeInForce == TimeInForce.Fok && qty < o.RemainingQuantity)
            return; // skip partial here; a bigger print may satisfy later

        var fillQty = o.TimeInForce == TimeInForce.Fok ? o.RemainingQuantity : qty;
        if (fillQty > 0m)
            o.ApplyFill(Quantity.From(fillQty), price);
    }
}
using Microsoft.Extensions.Logging;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
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
    private readonly ILogger<TradePrintExecutor> _log;
    private readonly ISlippageModel? _slippage;
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
            var rawPrice = print.Price;
            var tapePrice = Price.From(rawPrice);
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

                    var px = Adjust(rawPrice, qty, print);
                    ApplyFillWithLog(b, qty, px);
                    ApplyFillWithLog(s, qty, px);

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
                        ApplyFillWithLog(b, qty, Adjust(rawPrice, qty, print));
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
                        ApplyFillWithLog(s, qty, Adjust(rawPrice, qty, print));
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


    private decimal Adjust(decimal proposedPrice, decimal quantity, TradePrint print)
    {
        if (_slippage is null) return proposedPrice;
        // Build a basic snapshot using the print price on both sides
        var snap = new QuoteSnapshot(print.Symbol, proposedPrice, proposedPrice, proposedPrice, print.Timestamp);
        return _slippage.AdjustPrice(proposedPrice, quantity, snap);
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

    private void ApplyFillWithLog(Domain.Orders.Order o, decimal qty, decimal px)
    {
        var before = o.RemainingQuantity;
        ApplyFill(o, qty, Price.From(px));
        var after = o.RemainingQuantity;

        _log.LogInformation("Fill applied: OrderId={OrderId} User={UserId} Side={Side} Qty={Qty} Price={Price} Remaining {Before}->{After} State={State}",
            o.Id.Value, o.UserId, o.Side.ToString(), qty, px, before, after, o.State.ToString());
    }
}
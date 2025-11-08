using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Orders.Execution;

public sealed class SimpleMatchingEngine : IMatchingEngine
{
    private readonly IFillPolicy _policy;

    public SimpleMatchingEngine(IFillPolicy policy) => _policy = policy;

    public IEnumerable<ProposedFill> Match(Symbol symbol, QuoteSnapshot quote, IEnumerable<Order> openOrders)
    {
        // Execution prices
        var execBid = quote.Bid;
        var execAsk = quote.Ask;
        var last = quote.Last ?? (execBid + execAsk) / 2m;

        foreach (var o in openOrders)
        {
            if (o.State is not OrderState.Accepted and not OrderState.PartiallyFilled)
                continue;

            var remaining = o.RemainingQuantity;
            if (remaining <= 0) continue;

            bool cross = o.Type switch
            {
                OrderType.Market => true,
                OrderType.Limit => o.Side switch
                {
                    OrderSide.Buy  => o.LimitPrice!.Value >= execAsk,
                    OrderSide.Sell => o.LimitPrice!.Value <= execBid,
                    _ => false
                },
                _ => false
            };

            if (!cross) continue;

            var fillQty = _policy.DecideFillQuantity(remaining);
            if (fillQty <= 0) continue;

            var fillPrice = o.Side switch
            {
                OrderSide.Buy  => o.Type == OrderType.Market ? execAsk : Math.Min(execAsk, o.LimitPrice!.Value),
                OrderSide.Sell => o.Type == OrderType.Market ? execBid : Math.Max(execBid, o.LimitPrice!.Value),
                _ => last
            };

            yield return new ProposedFill(o, fillQty, fillPrice);
        }
    }
}
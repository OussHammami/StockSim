using System;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Portfolios;
using StockSim.Domain.Orders;
using StockSim.Domain.Orders.Events;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Orders.Handlers;

public sealed class OrderAcceptedHandler : IDomainEventHandler<OrderAccepted>
{
    private readonly IPortfolioRepository _repo;
    public OrderAcceptedHandler(IPortfolioRepository repo) => _repo = repo;

    public async Task HandleAsync(OrderAccepted e, CancellationToken ct = default)
    {
        var p = await _repo.GetByUserAsync(e.UserId, ct).ConfigureAwait(false);
        if (p is null) return;

        if (e.Side == OrderSide.Buy && e.Type == OrderType.Limit && e.LimitPrice is decimal lp)
        {
            var cost = Money.From(e.Quantity * lp);
            p.ReserveFunds(e.OrderId, cost);
            await _repo.SaveAsync(p, ct).ConfigureAwait(false);
            return;
        }

        if (e.Side == OrderSide.Sell)
        {
            p.ReserveShares(e.OrderId, e.Symbol, Quantity.From(e.Quantity));
            await _repo.SaveAsync(p, ct).ConfigureAwait(false);
        }
    }
}

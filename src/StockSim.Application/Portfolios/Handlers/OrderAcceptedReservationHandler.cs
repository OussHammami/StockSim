using StockSim.Application.Abstractions.Events;
using StockSim.Application.Orders;
using StockSim.Application.Portfolios;
using StockSim.Domain.Orders;
using StockSim.Domain.Orders.Events;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Portfolios.Handlers;

public sealed class OrderAcceptedReservationHandler : IDomainEventHandler<OrderAccepted>
{
    private readonly IOrderRepository _orders;
    private readonly IPortfolioRepository _portfolios;

    public OrderAcceptedReservationHandler(IOrderRepository orders, IPortfolioRepository portfolios)
    {
        _orders = orders;
        _portfolios = portfolios;
    }

    public async Task HandleAsync(OrderAccepted e, CancellationToken ct = default)
    {
        var order = await _orders.GetAsync(e.OrderId, ct).ConfigureAwait(false);
        if (order is null) return;

        var portfolio = await _portfolios.GetByUserAsync(order.UserId, ct).ConfigureAwait(false);
        if (portfolio is null) return;

        if (order.Side == OrderSide.Buy)
        {
            // Reserve max cost (limit or indicative ask). Safety: clamp.
            var unit = order.Type == OrderType.Limit && order.LimitPrice is not null
                ? order.LimitPrice.Value
                : (e.LimitPrice ?? 0m); // fallback
            if (unit <= 0m) return; // nothing to reserve if unknown
            portfolio.ReserveFunds(order.Id, Money.From(order.Quantity.Value * unit));
        }
        else
        {
            // Reserve shares for sell
            portfolio.ReserveShares(order.Id, order.Symbol, order.Quantity);
        }

        await _portfolios.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
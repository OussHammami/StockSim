using StockSim.Application.Abstractions.Events;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
using StockSim.Application.Portfolios;
using StockSim.Domain.Orders.Events;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Orders.Handlers;

public sealed class OrderPartiallyFilledHandler : IDomainEventHandler<OrderFillApplied>
{
    private readonly IOrderRepository _orders;
    private readonly IPortfolioRepository _portfolios;
    private readonly IIntegrationEventMapper _mapper;
    private readonly IOutboxWriter<IPortfolioOutboxContext> _outbox;

    public OrderPartiallyFilledHandler(
        IOrderRepository orders,
        IPortfolioRepository portfolios,
        IIntegrationEventMapper mapper,
        IOutboxWriter<IPortfolioOutboxContext> outbox)
    {
        _orders = orders;
        _portfolios = portfolios;
        _mapper = mapper;
        _outbox = outbox;
    }

    public async Task HandleAsync(OrderFillApplied e, CancellationToken ct = default)
    {
        // Need side, symbol, user → load order
        var order = await _orders.GetAsync(e.OrderId, ct).ConfigureAwait(false);
        if (order is null) return;

        var portfolio = await _portfolios.GetByUserAsync(order.UserId, ct).ConfigureAwait(false);
        if (portfolio is null) return;

        portfolio.ApplyFill(
            e.OrderId,
            order.Side,
            order.Symbol,
            Quantity.From(e.FillQuantity),
            Price.From(e.FillPrice));

        var ievents = _mapper.Map(portfolio.DomainEvents).ToArray();
        portfolio.ClearDomainEvents();

        await _outbox.WriteAsync(ievents, ct).ConfigureAwait(false);
        await _portfolios.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

using StockSim.Application.Abstractions.Events;
using StockSim.Application.Integration;
using StockSim.Application.Portfolios;
using StockSim.Domain.Orders;
using StockSim.Domain.Orders.Events;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Orders.Handlers;

public sealed class OrderAcceptedHandler : IDomainEventHandler<OrderAccepted>
{
    private readonly IPortfolioRepository _repo;
    private readonly IIntegrationEventMapper _mapper;
    private readonly IOutboxWriter _outbox;

    public OrderAcceptedHandler(IPortfolioRepository repo, IIntegrationEventMapper mapper, IOutboxWriter outbox)
    {
        _repo = repo;
        _mapper = mapper;
        _outbox = outbox;
    }

    public async Task HandleAsync(OrderAccepted e, CancellationToken ct = default)
    {
        var p = await _repo.GetByUserAsync(e.UserId, ct).ConfigureAwait(false);
        if (p is null) return;

        // Reserve based on side/type
        if (e.Side == OrderSide.Buy && e.Type == OrderType.Limit && e.LimitPrice is decimal lp)
        {
            var cost = Money.From(e.Quantity * lp);
            p.ReserveFunds(e.OrderId, cost);
        }
        else if (e.Side == OrderSide.Sell)
        {
            p.ReserveShares(e.OrderId, e.Symbol, Quantity.From(e.Quantity));
        }

        // Publish Portfolio domain events as integration events
        var ievents = _mapper.Map(p.DomainEvents).ToArray();
        p.ClearDomainEvents();

        await _outbox.WriteAsync(ievents, ct).ConfigureAwait(false);
        await _repo.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

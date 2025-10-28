using StockSim.Application.Abstractions.Events;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;
using StockSim.Application.Orders.Commands;

namespace StockSim.Application.Orders;

public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly IEventDispatcher _events;

    public OrderService(IOrderRepository orders, IEventDispatcher events)
    {
        _orders = orders;
        _events = events;
    }

    public async Task<OrderId> PlaceAsync(PlaceOrder cmd, CancellationToken ct = default)
    {
        var symbol = Symbol.From(cmd.Symbol);
        var qty = Quantity.From(cmd.Quantity);
        Order o = cmd.Type switch
        {
            OrderType.Market => Order.CreateMarket(cmd.UserId, symbol, cmd.Side, qty),
            OrderType.Limit  => Order.CreateLimit(cmd.UserId, symbol, cmd.Side, qty, Price.From(cmd.LimitPrice!.Value)),
            _ => throw new ArgumentOutOfRangeException(nameof(cmd.Type))
        };

        o.Accept(); // raises OrderAccepted

        await _orders.AddAsync(o, ct);
        await _events.DispatchAsync(o.DomainEvents, ct);
        o.ClearDomainEvents();
        await _orders.SaveChangesAsync(ct);

        return o.Id;
    }

    public async Task CancelAsync(CancelOrder cmd, CancellationToken ct = default)
    {
        var o = await _orders.GetAsync(cmd.OrderId, ct) ?? throw new InvalidOperationException("Order not found.");
        if (o.UserId != cmd.UserId) throw new InvalidOperationException("Forbidden.");
        o.Cancel(cmd.Reason);
        await _events.DispatchAsync(o.DomainEvents, ct);
        o.ClearDomainEvents();
        await _orders.SaveChangesAsync(ct);
    }

    public Task<Order?> GetAsync(OrderId id, CancellationToken ct = default) => _orders.GetAsync(id, ct);
}

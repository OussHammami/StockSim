using StockSim.Application.Abstractions.Events;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
using StockSim.Application.Orders.Commands;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;
using System.Diagnostics;

namespace StockSim.Application.Orders;

public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly IEventDispatcher _events;
    private readonly IIntegrationEventMapper _mapper;
    private readonly IOutboxWriter<ITradingOutboxContext> _outbox;

    public OrderService(IOrderRepository orders, IEventDispatcher events, IIntegrationEventMapper mapper, IOutboxWriter<ITradingOutboxContext> outbox)
    {
        _orders = orders;
        _events = events;
        _mapper = mapper;
        _outbox = outbox;
    }

    public async Task<OrderId> PlaceAsync(PlaceOrder cmd, CancellationToken ct = default)
    {
        using var act = Telemetry.Telemetry.OrdersSource.StartActivity("Order.Place", ActivityKind.Server)?
            .AddTag("user.id", cmd.UserId)
            .AddTag("symbol", cmd.Symbol)
            .AddTag("side", cmd.Side.ToString())
            .AddTag("type", cmd.Type.ToString())
            .AddTag("qty", cmd.Quantity);

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

        var integrationEvents = _mapper.Map(o.DomainEvents).ToArray();
        o.ClearDomainEvents();

        await _outbox.WriteAsync(integrationEvents, ct);
        await _orders.SaveChangesAsync(ct);

        Telemetry.Telemetry.OrdersPlaced.Add(1);
        return o.Id;
    }

    public async Task CancelAsync(CancelOrder cmd, CancellationToken ct = default)
    {
        using var act = Telemetry.Telemetry.OrdersSource.StartActivity("Order.Cancel", ActivityKind.Server)?
            .AddTag("user.id", cmd.UserId)
            .AddTag("order.id", cmd.OrderId)
            .AddTag("reason", cmd.Reason);

        var o = await _orders.GetAsync(cmd.OrderId, ct) ?? throw new InvalidOperationException("Order not found.");
        if (o.UserId != cmd.UserId) throw new InvalidOperationException("Forbidden.");
        o.Cancel(cmd.Reason);
        await _events.DispatchAsync(o.DomainEvents, ct);
        var integrationEvents = _mapper.Map(o.DomainEvents).ToArray();
        o.ClearDomainEvents();
        
        await _outbox.WriteAsync(integrationEvents, ct);
        await _orders.SaveChangesAsync(ct);

        Telemetry.Telemetry.OrdersCanceled.Add(1);
    }

    public Task<Order?> GetAsync(OrderId id, CancellationToken ct = default) => _orders.GetAsync(id, ct);

    public Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default) => _orders.GetByUserAsync(userId, skip, take, ct: ct);
}

using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Orders.Events;

public sealed class OrderAccepted : IDomainEvent
{
    public Guid UserId { get; }
    public OrderId OrderId { get; }
    public Symbol Symbol { get; }
    public OrderSide Side { get; }
    public decimal Quantity { get; }
    public OrderType Type { get; }
    public decimal? LimitPrice { get; }
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public OrderAccepted(Guid userId, OrderId orderId, Symbol symbol, OrderSide side, decimal quantity,
        OrderType type, decimal? limitPrice)
    {
        UserId = userId;
        OrderId = orderId;
        Symbol = symbol;
        Side = side;
        Quantity = quantity;
        Type = type;
        LimitPrice = limitPrice;
    }
}

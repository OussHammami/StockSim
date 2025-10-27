using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Orders.Events;

public sealed class OrderRejected : IDomainEvent
{
    public OrderId OrderId { get; }
    public string Reason { get; }
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public OrderRejected(OrderId orderId, string reason)
    {
        OrderId = orderId;
        Reason = reason;
    }
}

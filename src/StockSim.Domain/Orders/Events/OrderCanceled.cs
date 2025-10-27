using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Orders.Events;

public sealed class OrderCanceled : IDomainEvent
{
    public OrderId OrderId { get; }
    public string? Reason { get; }
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public OrderCanceled(OrderId orderId, string? reason)
    {
        OrderId = orderId;
        Reason = reason;
    }
}

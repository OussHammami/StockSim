using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Orders.Events;

public sealed class OrderFilled : IDomainEvent
{
    public OrderId OrderId { get; }
    public decimal TotalFilledQuantity { get; }
    public decimal AverageFillPrice { get; }
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public OrderFilled(OrderId orderId, decimal totalQty, decimal avgPrice)
    {
        OrderId = orderId;
        TotalFilledQuantity = totalQty;
        AverageFillPrice = avgPrice;
    }
}

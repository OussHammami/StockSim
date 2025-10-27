using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Orders.Events;

public sealed class OrderPartiallyFilled : IDomainEvent
{
    public OrderId OrderId { get; }
    public decimal FillQuantity { get; }
    public decimal FillPrice { get; }
    public decimal CumFilledQuantity { get; }
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public OrderPartiallyFilled(OrderId orderId, decimal fillQty, decimal fillPrice, decimal cumFilled)
    {
        OrderId = orderId;
        FillQuantity = fillQty;
        FillPrice = fillPrice;
        CumFilledQuantity = cumFilled;
    }
}

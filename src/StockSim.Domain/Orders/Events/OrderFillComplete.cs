using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Orders.Events;

public sealed class OrderFillComplete : IDomainEvent
{
    public OrderId OrderId { get; }
    public Guid UserId { get; }
    public Symbol Symbol { get; }
    public OrderSide Side { get; }
    public decimal TotalFilledQuantity { get; }
    public decimal AverageFillPrice { get; }
    public DateTimeOffset OccurredAt { get; }

    public OrderFillComplete(Guid userId, OrderId orderId, Symbol symbol, OrderSide side, decimal totalQty, decimal avgPrice, DateTimeOffset? occurredAt = null)
    {
        OrderId = orderId;
        UserId = userId;
        Symbol = symbol;
        Side = side;
        TotalFilledQuantity = totalQty;
        AverageFillPrice = avgPrice;
        OccurredAt = occurredAt ?? DateTimeOffset.UtcNow;
    }
}

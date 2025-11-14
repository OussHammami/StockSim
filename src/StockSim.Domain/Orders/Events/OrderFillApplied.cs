using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Orders.Events;

public sealed class OrderFillApplied : IDomainEvent
{

    public OrderId OrderId { get; }
    public Guid UserId { get; }
    public Symbol Symbol { get; }
    public OrderSide Side { get; }
    public decimal FillQuantity { get; }
    public decimal FillPrice { get; }
    public decimal CumFilledQuantity { get; }
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public OrderFillApplied(Guid userId, OrderId orderId, Symbol symbol, OrderSide side, decimal fillQty, decimal fillPrice, decimal cumFilled, DateTimeOffset? occurredAt = null)
    {
        UserId = userId;
        OrderId = orderId;
        Symbol = symbol;
        Side = side;
        FillQuantity = fillQty;
        FillPrice = fillPrice;
        CumFilledQuantity = cumFilled;
        OccurredAt = occurredAt ?? DateTimeOffset.UtcNow;
    }
}

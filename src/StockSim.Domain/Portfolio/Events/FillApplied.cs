using StockSim.Domain.Orders;
using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Portfolio.Events;

public sealed class FillApplied : IDomainEvent
{
    public PortfolioId PortfolioId { get; }
    public OrderId OrderId { get; }
    public OrderSide Side { get; }
    public Symbol Symbol { get; }
    public Quantity Quantity { get; }
    public Price Price { get; }
    public Money CashDelta { get; }
    public decimal NewPositionQty { get; }
    public decimal NewAvgCost { get; }
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public FillApplied(PortfolioId pid, OrderId oid, OrderSide side, Symbol symbol, Quantity qty, Price price,
        Money cashDelta, decimal newPosQty, decimal newAvgCost)
    {
        PortfolioId = pid;
        OrderId = oid;
        Side = side;
        Symbol = symbol;
        Quantity = qty;
        Price = price;
        CashDelta = cashDelta;
        NewPositionQty = newPosQty;
        NewAvgCost = newAvgCost;
    }
}

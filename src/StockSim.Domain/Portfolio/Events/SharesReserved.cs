using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Portfolio.Events;

public sealed class SharesReserved : IDomainEvent
{
    public PortfolioId PortfolioId { get; }
    public OrderId OrderId { get; }
    public Symbol Symbol { get; }
    public Quantity Quantity { get; }
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public SharesReserved(PortfolioId pid, OrderId oid, Symbol symbol, Quantity qty)
    {
        PortfolioId = pid;
        OrderId = oid;
        Symbol = symbol;
        Quantity = qty;
    }
}

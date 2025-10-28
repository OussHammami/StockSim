using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Portfolio.Events;

public sealed class FundsReserved : IDomainEvent
{
    public PortfolioId PortfolioId { get; }
    public OrderId OrderId { get; }
    public Money Amount { get; }
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public FundsReserved(PortfolioId pid, OrderId oid, Money amount)
    {
        PortfolioId = pid;
        OrderId = oid;
        Amount = amount;
    }
}

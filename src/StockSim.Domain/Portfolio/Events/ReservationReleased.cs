using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Portfolio.Events;

public class ReservationReleased: IDomainEvent
{
    public PortfolioId PortfolioId { get; }
    public OrderId OrderId { get; }
    public Money? Funds { get; }
    public Symbol? Symbol { get; }
    public Quantity? Shares { get; }
    public string Reason { get; }
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    public static ReservationReleased FundsOnly(PortfolioId pid, OrderId oid, Money amount, string reason) =>
        new(pid, oid, amount, null, null, reason);

    public static ReservationReleased SharesOnly(PortfolioId pid, OrderId oid, Symbol symbol, Quantity qty, string reason) =>
        new(pid, oid, null, symbol, qty, reason);

    private ReservationReleased(PortfolioId pid, OrderId oid, Money? funds, Symbol? symbol, Quantity? shares, string reason)
    {
        PortfolioId = pid;
        OrderId = oid;
        Funds = funds;
        Symbol = symbol;
        Shares = shares;
        Reason = reason;
    }
}

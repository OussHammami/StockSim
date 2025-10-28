using StockSim.Domain.Orders;
using StockSim.Domain.Portfolio.Events;
using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Portfolio;

public sealed class Portfolio : Entity
{
    public PortfolioId Id { get; }
    public Guid UserId { get; }

    // cash
    public Money Cash { get; private set; } = Money.Zero;
    public Money ReservedCash { get; private set; } = Money.Zero;

    // positions
    private readonly List<Position> _positions = new();
    public IReadOnlyCollection<Position> Positions => _positions.AsReadOnly();
    private readonly Dictionary<string, decimal> _reservedShares = new(); // by symbol

    public Portfolio(PortfolioId id, Guid userId)
    {
        Id = id;
        UserId = userId;
    }

    public void Deposit(Money amount)
    {
        Cash = Cash.Add(amount);
    }

    public void Withdraw(Money amount)
    {
        var newCash = Cash.Subtract(amount);
        if (newCash.Amount < 0m) throw new InvalidOperationException("Insufficient cash.");
        Cash = newCash;
    }

    public void ReserveFunds(OrderId orderId, Money amount)
    {
        if (amount.Amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount));
        if (AvailableCash().Amount < amount.Amount) throw new InvalidOperationException("Insufficient available cash.");
        ReservedCash = Money.From(ReservedCash.Amount + amount.Amount);
        Raise(new FundsReserved(Id, orderId, amount));
    }

    public void ReleaseFunds(OrderId orderId, Money amount, string reason)
    {
        var release = Math.Min(amount.Amount, ReservedCash.Amount);
        ReservedCash = Money.From(ReservedCash.Amount - release);
        if (release > 0m)
            Raise(ReservationReleased.FundsOnly(Id, orderId, Money.From(release), reason));
    }

    public void ReserveShares(OrderId orderId, Symbol symbol, Quantity qty)
    {
        var pos = GetOrCreate(symbol);
        var available = pos.Quantity - (_reservedShares.TryGetValue(symbol, out var r) ? r : 0m);
        if (qty.Value > available) throw new InvalidOperationException("Insufficient shares to reserve.");
        _reservedShares[symbol] = _reservedShares.GetValueOrDefault(symbol) + qty.Value;
        Raise(new SharesReserved(Id, orderId, symbol, qty));
    }

    public void ReleaseShares(OrderId orderId, Symbol symbol, Quantity qty, string reason)
    {
        var cur = _reservedShares.GetValueOrDefault(symbol);
        var release = Math.Min(qty.Value, cur);
        if (release > 0m)
        {
            _reservedShares[symbol] = cur - release;
            if (_reservedShares[symbol] == 0m) _reservedShares.Remove(symbol);
            Raise(ReservationReleased.SharesOnly(Id, orderId, symbol, Quantity.From(release), reason));
        }
    }

    public void ApplyFill(OrderId orderId, OrderSide side, Symbol symbol, Quantity qty, Price price)
    {
        var pos = GetOrCreate(symbol);
        Money cashDelta;

        if (side == OrderSide.Buy)
        {
            // spend cash, consume reserved first
            var cost = Money.From(qty.Value * price.Value);
            if (Cash.Amount < cost.Amount) throw new InvalidOperationException("Insufficient cash for fill.");
            ReleaseFunds(orderId, cost, "buy fill");
            Cash = Cash.Subtract(cost);
            pos.ApplyBuy(qty, price);
            cashDelta = Money.From(-cost.Amount);
        }
        else
        {
            // receive cash, reduce position and reserved shares
            if (qty.Value > pos.Quantity) throw new InvalidOperationException("Insufficient shares for fill.");
            pos.ApplySell(qty);
            ReleaseShares(orderId, symbol, qty, "sell fill");
            var proceeds = Money.From(qty.Value * price.Value);
            Cash = Cash.Add(proceeds);
            cashDelta = proceeds;
        }

        Raise(new FillApplied(Id, orderId, side, symbol, qty, price, cashDelta, pos.Quantity, pos.AvgCost));
    }

    public Money AvailableCash() => Money.From(Cash.Amount - ReservedCash.Amount);

    public decimal ReservedFor(Symbol symbol) => _reservedShares.GetValueOrDefault(symbol);
    public Position? GetPosition(string symbolValue) => _positions.FirstOrDefault(p => p.Symbol.Value == symbolValue);
    private Position GetOrCreate(Symbol symbol)
    {
        var existing = _positions.FirstOrDefault(p => p.Symbol.Value == symbol.Value);
        if (existing is not null) return existing;
        var p = new Position(symbol);
        _positions.Add(p);
        return p;
    }
}

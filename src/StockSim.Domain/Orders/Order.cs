using StockSim.Domain.Orders.Events;
using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Orders;

public sealed class Order: Entity
{
    public OrderId Id { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public Symbol Symbol { get; private set; } = null!;
    public OrderSide Side { get; private set; }
    public OrderType Type { get; private set; }
    public Quantity Quantity { get; private set; } = null!;
    public Price? LimitPrice { get; private set; }

    public OrderState State { get; private set; } = OrderState.New;

    public decimal FilledQuantity { get; private set; }
    public decimal RemainingQuantity => Quantity.Value - FilledQuantity;
    public decimal AverageFillPrice { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private Order() { }

    private Order(OrderId id, Guid userId, Symbol symbol, OrderSide side, OrderType type, Quantity qty, Price? limitPrice)
    {
        if (type == OrderType.Limit && limitPrice is null)
            throw new ArgumentException("Limit orders require a limit price.", nameof(limitPrice));
        if (type == OrderType.Market && limitPrice is not null)
            throw new ArgumentException("Market orders cannot carry a limit price.", nameof(limitPrice));

        Id = id;
        UserId = userId;
        Symbol = symbol;
        Side = side;
        Type = type;
        Quantity = qty;
        LimitPrice = limitPrice;
        CreatedAt = DateTime.UtcNow;
    }

    public static Order CreateLimit(Guid userId, Symbol symbol, OrderSide side, Quantity quantity, Price limitPrice)
        => new(OrderId.New(), userId, symbol, side, OrderType.Limit, quantity, limitPrice);

    public static Order CreateMarket(Guid userId, Symbol symbol, OrderSide side, Quantity quantity)
        => new(OrderId.New(), userId, symbol, side, OrderType.Market, quantity, null);

    public void Accept()
    {
        EnsureState(OrderState.New);
        State = OrderState.Accepted;
        Raise(new OrderAccepted(UserId, Id, Symbol, Side, Quantity.Value, Type, LimitPrice?.Value));
    }

    public void ApplyFill(Quantity fillQty, Price fillPrice)
    {
        if (State is not OrderState.Accepted and not OrderState.PartiallyFilled)
            throw new InvalidOperationException($"Cannot fill order in state {State}.");

        if (fillQty.Value <= 0m) throw new ArgumentOutOfRangeException(nameof(fillQty), "Fill quantity must be > 0.");
        if (fillQty.Value > RemainingQuantity)
            throw new InvalidOperationException("Fill quantity exceeds remaining.");

        // running average price
        var newCumQty = FilledQuantity + fillQty.Value;
        var newCumAmount = (AverageFillPrice * FilledQuantity) + (fillPrice.Value * fillQty.Value);
        AverageFillPrice = newCumQty == 0 ? 0 : decimal.Round(newCumAmount / newCumQty, 4, MidpointRounding.AwayFromZero);
        FilledQuantity = newCumQty;

        if (RemainingQuantity == 0m)
        {
            State = OrderState.Filled;
            Raise(new OrderFilled(Id, FilledQuantity, AverageFillPrice));
        }
        else
        {
            State = OrderState.PartiallyFilled;
            Raise(new OrderPartiallyFilled(Id, fillQty.Value, fillPrice.Value, FilledQuantity));
        }
    }

    public void Reject(string reason)
    {
        if (State is OrderState.Filled or OrderState.Canceled or OrderState.Rejected)
            throw new InvalidOperationException($"Cannot reject order in state {State}.");

        State = OrderState.Rejected;
        Raise(new OrderRejected(Id, string.IsNullOrWhiteSpace(reason) ? "rejected" : reason));
    }

    public void Cancel(string? reason = null)
    {
        if (State is OrderState.Filled or OrderState.Canceled or OrderState.Rejected)
            throw new InvalidOperationException($"Cannot cancel order in state {State}.");

        State = OrderState.Canceled;
        Raise(new OrderCanceled(Id, reason));
    }

    private void EnsureState(params OrderState[] allowed)
    {
        if (!allowed.Contains(State))
            throw new InvalidOperationException($"State {State} not allowed here.");
    }
}

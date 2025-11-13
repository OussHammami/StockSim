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
    public TimeInForce TimeInForce { get; private set; } = TimeInForce.Gtc;
    public DateTimeOffset? ExpiresAt { get; private set; }

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

    public static Order CreateLimit(Guid userId, Symbol symbol, OrderSide side, Quantity quantity, Price limitPrice, TimeInForce tif = TimeInForce.Gtc)
    {
        var o = new Order(OrderId.New(), userId, symbol, side, OrderType.Limit, quantity, limitPrice);
        o.ConfigureTimeInForce(tif);
        return o;
    }

    public static Order CreateMarket(Guid userId, Symbol symbol, OrderSide side, Quantity quantity, TimeInForce tif = TimeInForce.Gtc)
    {
        var o = new Order(OrderId.New(), userId, symbol, side, OrderType.Market, quantity, null);
        o.ConfigureTimeInForce(tif);
        return o;
    }

    public bool IsExpired(DateTimeOffset nowUtc) =>
        ExpiresAt.HasValue && nowUtc >= ExpiresAt && State is OrderState.Accepted or OrderState.PartiallyFilled;

    public bool RequiresAllOrNothing => TimeInForce == TimeInForce.Fok;

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
            Raise(new OrderFilled(UserId, Id, Symbol, Side, FilledQuantity, AverageFillPrice));
        }
        else
        {
            State = OrderState.PartiallyFilled;
            Raise(new OrderPartiallyFilled(UserId, Id, Symbol, Side, fillQty.Value, fillPrice.Value, FilledQuantity));
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
    private void ConfigureTimeInForce(TimeInForce tif)
    {
        TimeInForce = tif;
        if (tif == TimeInForce.Day)
        {
            // naive: end of current UTC day
            var utcNow = DateTimeOffset.UtcNow;
            ExpiresAt = new DateTimeOffset(utcNow.Year, utcNow.Month, utcNow.Day, 23, 59, 59, TimeSpan.Zero);
        }
        else if (tif is TimeInForce.Ioc or TimeInForce.Fok)
        {
            // immediate evaluation; no expiration timestamp needed
            ExpiresAt = null;
        }
    }
}

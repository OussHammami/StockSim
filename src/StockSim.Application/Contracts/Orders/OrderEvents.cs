namespace StockSim.Application.Contracts.Orders;

public abstract record OrderEvent(Guid OrderId, string UserId, string Symbol, int Quantity, DateTimeOffset TimeUtc);
public sealed record OrderFilledEvent(Guid OrderId, string UserId, string Symbol, int Quantity, decimal FillPrice, DateTimeOffset TimeUtc)
    : OrderEvent(OrderId, UserId, Symbol, Quantity, TimeUtc);
public sealed record OrderRejectedEvent(Guid OrderId, string UserId, string Symbol, int Quantity, string Reason, DateTimeOffset TimeUtc)
    : OrderEvent(OrderId, UserId, Symbol, Quantity, TimeUtc);

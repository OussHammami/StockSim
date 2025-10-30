using StockSim.Domain.Orders;

namespace StockSim.Application.Orders.Commands;

public sealed record PlaceOrder(
    Guid UserId,
    string Symbol,
    OrderSide Side,
    OrderType Type,
    decimal Quantity,
    decimal? LimitPrice);

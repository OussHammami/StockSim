using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Orders.Commands;

public sealed record CancelOrder(Guid UserId, OrderId OrderId, string? Reason);

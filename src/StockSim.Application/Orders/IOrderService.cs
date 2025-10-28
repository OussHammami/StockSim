using StockSim.Application.Orders.Commands;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Orders;

public interface IOrderService
{
    Task<OrderId> PlaceAsync(PlaceOrder cmd, CancellationToken ct = default);
    Task CancelAsync(CancelOrder cmd, CancellationToken ct = default);
    Task<Order?> GetAsync(OrderId id, CancellationToken ct = default);
}

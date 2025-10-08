using StockSim.Domain.Entities;

namespace StockSim.Application.Abstractions;

public interface IOrderService
{
    Task<IReadOnlyList<Order>> GetRecentAsync(string userId, int take = 50, CancellationToken ct = default);
}

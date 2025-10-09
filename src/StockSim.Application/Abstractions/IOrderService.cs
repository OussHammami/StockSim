using StockSim.Domain.Entities;

namespace StockSim.Application.Abstractions;
public sealed record PageResult<T>(IReadOnlyList<T> Items, int Total);

public interface IOrderService
{
    Task<PageResult<Order>> GetPageAsync(string userId, int skip, int take, CancellationToken ct = default);
}

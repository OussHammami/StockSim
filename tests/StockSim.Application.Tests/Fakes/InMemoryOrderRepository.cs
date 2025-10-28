using StockSim.Application.Orders;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Tests.Fakes;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly Dictionary<string, Order> _byId = new();

    public Task<Order?> GetAsync(OrderId id, CancellationToken ct = default) =>
        Task.FromResult(_byId.TryGetValue(id.ToString(), out var o) ? o : null);

    public Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Order>>(_byId.Values.Where(o => o.UserId == userId).Skip(skip).Take(take).ToList());

    public Task AddAsync(Order order, CancellationToken ct = default)
    {
        _byId[order.Id.ToString()] = order;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

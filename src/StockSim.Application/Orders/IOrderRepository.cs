using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Orders;

public interface IOrderRepository
{
    Task<Order?> GetAsync(OrderId id, CancellationToken ct = default);

    /// <summary>Optional helper to page a user's recent orders.</summary>
    Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default);

    Task<IReadOnlyList<Order>> GetOpenBySymbolAsync(Symbol symbol, CancellationToken ct = default);

    Task AddAsync(Order order, CancellationToken ct = default);

    /// <summary>For ORMs that require explicit persistence.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetAllOpenAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Symbol>> GetSymbolsWithOpenAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetOpenBuysAtOrAboveAsync(Symbol symbol, decimal price, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetOpenSellsAtOrBelowAsync(Symbol symbol, decimal price, CancellationToken ct = default);

}

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

    public Task<IReadOnlyList<Order>> GetOpenBySymbolAsync(Symbol symbol, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Order>>(
            _byId.Values
                .Where(o => o.Symbol == symbol &&
                            (o.State == OrderState.Accepted || o.State == OrderState.PartiallyFilled) &&
                            o.Quantity.Value > o.FilledQuantity)
                .OrderBy(o => o.CreatedAt)
                .ToList());

    public Task<IReadOnlyList<Order>> GetAllOpenAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Order>>(
            _byId.Values
                .Where(o => (o.State == OrderState.Accepted || o.State == OrderState.PartiallyFilled) &&
                            o.Quantity.Value > o.FilledQuantity)
                .ToList());


    public Task<IReadOnlyList<Symbol>> GetSymbolsWithOpenAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Symbol>>(
            _byId.Values
                .Where(o => (o.State == OrderState.Accepted || o.State == OrderState.PartiallyFilled) &&
                            o.Quantity.Value > o.FilledQuantity)
                .Select(o => o.Symbol)
                .Distinct()
                .ToList());

    public Task<IReadOnlyList<Order>> GetOpenBuysAtOrAboveAsync(Symbol symbol, decimal price, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Order>>(
            _byId.Values
                .Where(o => o.Symbol == symbol &&
                            o.Side == OrderSide.Buy &&
                            (o.State == OrderState.Accepted || o.State == OrderState.PartiallyFilled) &&
                            o.Quantity.Value > o.FilledQuantity &&
                            (o.Type == OrderType.Market || (o.LimitPrice != null && o.LimitPrice.Value >= price)))
                .ToList());

    public Task<IReadOnlyList<Order>> GetOpenSellsAtOrBelowAsync(Symbol symbol, decimal price, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Order>>(
            _byId.Values
                .Where(o => o.Symbol == symbol &&
                            o.Side == OrderSide.Sell &&
                            (o.State == OrderState.Accepted || o.State == OrderState.PartiallyFilled) &&
                            o.Quantity.Value > o.FilledQuantity &&
                            (o.Type == OrderType.Market || (o.LimitPrice != null && o.LimitPrice.Value <= price)))
                .ToList());
}

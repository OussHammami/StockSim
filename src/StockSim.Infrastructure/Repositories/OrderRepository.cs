using Microsoft.EntityFrameworkCore;
using StockSim.Application.Orders;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;
using StockSim.Infrastructure.Persistence.Trading;

namespace StockSim.Infrastructure.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly TradingDbContext _db;
    public OrderRepository(TradingDbContext db) => _db = db;

    public Task<Order?> GetAsync(OrderId id, CancellationToken ct = default) =>
        _db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default) =>
        await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip(Math.Max(0, skip)).Take(Math.Clamp(take, 1, 200))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetOpenBySymbolAsync(Symbol symbol, CancellationToken ct = default) =>
    await _db.Orders.AsNoTracking()
        .Where(o => o.Symbol.Value == symbol.Value &&
                    (o.State == OrderState.Accepted || o.State == OrderState.PartiallyFilled) &&
                    o.Quantity.Value > o.FilledQuantity)
        .OrderBy(o => o.CreatedAt)
        .ToListAsync(ct);
    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _db.Orders.AddAsync(order, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<Order>> GetAllOpenAsync(CancellationToken ct = default) =>
    await _db.Orders
        .Where(o => (o.State == OrderState.Accepted || o.State == OrderState.PartiallyFilled) &&
                    o.Quantity.Value > o.FilledQuantity)
        .ToListAsync(ct);

    public async Task<IReadOnlyList<Symbol>> GetSymbolsWithOpenAsync(CancellationToken ct = default) =>
        await _db.Orders
            .Where(o => (o.State == OrderState.Accepted || o.State == OrderState.PartiallyFilled) &&
                        o.Quantity.Value > o.FilledQuantity)
            .Select(o => o.Symbol)
            .Distinct()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetOpenBuysAtOrAboveAsync(Symbol symbol, decimal price, CancellationToken ct = default) =>
        await _db.Orders
            .Where(o => o.Symbol.Value == symbol.Value &&
                        o.Side == OrderSide.Buy &&
                        (o.State == OrderState.Accepted || o.State == OrderState.PartiallyFilled) &&
                        o.Quantity.Value > o.FilledQuantity &&
                        (o.Type == OrderType.Market || (o.LimitPrice != null && o.LimitPrice.Value >= price)))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> GetOpenSellsAtOrBelowAsync(Symbol symbol, decimal price, CancellationToken ct = default) =>
        await _db.Orders
            .Where(o => o.Symbol.Value == symbol.Value &&
                        o.Side == OrderSide.Sell &&
                        (o.State == OrderState.Accepted || o.State == OrderState.PartiallyFilled) &&
                        o.Quantity.Value > o.FilledQuantity &&
                        (o.Type == OrderType.Market || (o.LimitPrice != null && o.LimitPrice.Value <= price)))
            .ToListAsync(ct);
}

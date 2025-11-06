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
        _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Order>> GetByUserAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default) =>
        await _db.Orders.AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => EF.Property<Guid>(o, "Id"))
            .Skip(Math.Max(0, skip)).Take(Math.Clamp(take, 1, 200))
            .ToListAsync(ct);

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _db.Orders.AddAsync(order, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

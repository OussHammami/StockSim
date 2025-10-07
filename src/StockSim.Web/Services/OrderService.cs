using Microsoft.EntityFrameworkCore;
using StockSim.Web.Data;
using StockSim.Web.Data.Trading;

namespace StockSim.Web.Services;
public interface IOrderService
{
    Task<IReadOnlyList<OrderEntity>> GetRecentAsync(string userId, int take = 50, CancellationToken ct = default);
}

public sealed class OrderService(IServiceScopeFactory scopes) : IOrderService
{
    public async Task<IReadOnlyList<OrderEntity>> GetRecentAsync(string userId, int take = 50, CancellationToken ct = default)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.SubmittedUtc)
            .Take(take)
            .ToListAsync(ct);
    }
}


using Microsoft.EntityFrameworkCore;
using StockSim.Application.Abstractions.Paging;
using StockSim.Domain.Entities;
using StockSim.Domain.Enums;
using StockSim.Infrastructure.Persistence;

namespace StockSim.Web.Services;

public sealed class OrderQueries(ApplicationDbContext db) : IOrderQueries
{
    public async Task<PageResult<Order>> GetPageAsync(string userId, int skip, int take, CancellationToken ct = default)
    {
        var q = db.Orders.Where(o => o.UserId == userId);
        var total = await q.CountAsync(ct);

        var items = await q.OrderByDescending(o => o.SubmittedUtc)
            .Skip(skip).Take(take)
            .Select(o => new Order
            {
                OrderId = o.OrderId,
                UserId = o.UserId,
                Symbol = o.Symbol,
                Quantity = o.Quantity,
                SubmittedUtc = o.SubmittedUtc,
                Status = (OrderStatus)o.Status,
                FillPrice = o.FillPrice,
                FilledUtc = o.FilledUtc
            })
            .ToListAsync(ct);

        return new PageResult<Order>(items, total);
    }
}


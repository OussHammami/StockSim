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
            .Select(e => new Order
            {
                OrderId = e.OrderId,
                Symbol = e.Symbol,
                Quantity = e.Quantity,
                Status = e.Status,
                FillPrice = e.FillPrice,
                SubmittedUtc = e.SubmittedUtc,
                FilledUtc = e.FilledUtc,
                Type = e.Type,
                Tif = e.Tif,
                LimitPrice = e.LimitPrice,
                StopPrice = e.StopPrice,
                Remaining = e.Remaining
            })
            .ToListAsync(ct);

        return new PageResult<Order>(items, total);
    }
}


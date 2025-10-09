using StockSim.Application.Abstractions.Paging;
using StockSim.Domain.Entities;

public interface IOrderQueries
{
    Task<PageResult<Order>> GetPageAsync(string userId, int skip, int take, CancellationToken ct = default);
}

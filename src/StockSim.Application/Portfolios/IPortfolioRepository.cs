using StockSim.Domain.Portfolio;

namespace StockSim.Application.Portfolios;

public interface IPortfolioRepository
{
    Task<Portfolio?> GetByUserAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(Portfolio portfolio, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

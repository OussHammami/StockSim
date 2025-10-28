using StockSim.Domain.Portfolio;

namespace StockSim.Application.Portfolios;

public interface IPortfolioRepository
{
    Task<Portfolio?> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task SaveAsync(Portfolio portfolio, CancellationToken ct = default);
}

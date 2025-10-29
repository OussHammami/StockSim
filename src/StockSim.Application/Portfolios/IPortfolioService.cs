using StockSim.Domain.Portfolio;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Portfolios;

public interface IPortfolioService
{
    Task<Portfolio> GetOrCreateAsync(Guid userId, CancellationToken ct = default);
    Task DepositAsync(Guid userId, Money amount, CancellationToken ct = default);
}

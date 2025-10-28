using StockSim.Application.Portfolios;
using StockSim.Domain.Portfolio;

namespace StockSim.Application.Tests.Fakes;

public sealed class InMemoryPortfolioRepository : IPortfolioRepository
{
    private readonly Dictionary<Guid, Portfolio> _store = new();

    public Task<Portfolio?> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(userId, out var p) ? p : null);

    public Task SaveAsync(Portfolio portfolio, CancellationToken ct = default)
    {
        _store[portfolio.UserId] = portfolio;
        return Task.CompletedTask;
    }

    public void Seed(Portfolio p) => _store[p.UserId] = p;
}

using StockSim.Application.Portfolios;
using StockSim.Domain.Portfolio;

namespace StockSim.Web.IntegrationTests.Fakes;

public sealed class InMemoryPortfolioRepository : IPortfolioRepository
{
    private readonly Dictionary<Guid, Domain.Portfolio.Portfolio> _store = new();

    public Task<Domain.Portfolio.Portfolio?> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult(_store.TryGetValue(userId, out var p) ? p : null);

    public Task AddAsync(Domain.Portfolio.Portfolio portfolio, CancellationToken ct = default)
    {
        _store[portfolio.UserId] = portfolio;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

    public void Seed(Domain.Portfolio.Portfolio p) => _store[p.UserId] = p;
}

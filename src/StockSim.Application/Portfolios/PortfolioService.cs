using StockSim.Domain.Portfolio;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Portfolios;

public sealed class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _repo;
    public PortfolioService(IPortfolioRepository repo) => _repo = repo;

    public async Task<Portfolio> GetOrCreateAsync(Guid userId, CancellationToken ct = default)
    {
        var p = await _repo.GetByUserAsync(userId, ct);
        if (p is not null) return p;

        p = new Portfolio(PortfolioId.New(), userId);
        await _repo.AddAsync(p, ct);
        await _repo.SaveChangesAsync(ct);
        return p;
    }

    public async Task DepositAsync(Guid userId, Money amount, CancellationToken ct = default)
    {
        var p = await GetOrCreateAsync(userId, ct);
        p.Deposit(amount);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Position>> GetPositionsAsync(Guid userId, CancellationToken ct = default)
    {
        var p = await GetOrCreateAsync(userId, ct);
        return p.Positions.OrderBy(ps => ps.Symbol).ToList();
    }
}

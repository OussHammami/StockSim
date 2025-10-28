using Microsoft.EntityFrameworkCore;
using StockSim.Application.Portfolios;
using StockSim.Domain.Portfolio;
using StockSim.Infrastructure.Persistence.Portfolioing;
    
namespace StockSim.Infrastructure.Repositories;

public sealed class PortfolioRepository : IPortfolioRepository
{
    private readonly PortfolioDbContext _db;
    public PortfolioRepository(PortfolioDbContext db) => _db = db;

    public Task<Portfolio?> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        _db.Portfolios
            .Include(p => p.Positions)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public Task AddAsync(Portfolio portfolio, CancellationToken ct = default) =>
        _db.Portfolios.AddAsync(portfolio, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

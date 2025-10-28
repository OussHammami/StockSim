using Microsoft.EntityFrameworkCore;

namespace StockSim.Infrastructure.Persistence.Portfolioing;

public sealed class PortfolioDbContext : DbContext
{
    public const string Schema = "portfolio";

    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options) { }

    public DbSet<Domain.Portfolio.Portfolio> Portfolios => Set<Domain.Portfolio.Portfolio>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema(Schema);
        b.ApplyConfigurationsFromAssembly(typeof(PortfolioDbContext).Assembly);
    }
}

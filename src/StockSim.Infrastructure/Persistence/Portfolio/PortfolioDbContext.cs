using Microsoft.EntityFrameworkCore;
using StockSim.Domain.Portfolio;
using StockSim.Infrastructure.Inbox;
using StockSim.Infrastructure.Outbox;

namespace StockSim.Infrastructure.Persistence.Portfolioing;

public sealed class PortfolioDbContext : DbContext
{
    public const string Schema = "portfolio";

    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options) { }

    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema(Schema);
        b.ApplyConfigurationsFromAssembly(typeof(PortfolioDbContext).Assembly);
    }
}

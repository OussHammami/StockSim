using Microsoft.EntityFrameworkCore;
using StockSim.Domain.Orders;

namespace StockSim.Infrastructure.Persistence.Trading;

public class TradingDbContext: DbContext
{
    public const string Schema = "trading";

    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema(Schema);
        b.ApplyConfigurationsFromAssembly(typeof(TradingDbContext).Assembly);
    }
}

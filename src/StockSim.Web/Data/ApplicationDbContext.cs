using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockSim.Web.Data.Trading;

namespace StockSim.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Trading.PortfolioEntity> Portfolios => Set<Trading.PortfolioEntity>();
    public DbSet<Trading.PositionEntity> Positions => Set<Trading.PositionEntity>();
    public DbSet<Trading.OrderEntity> Orders => Set<Trading.OrderEntity>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Trading.PortfolioEntity>()
            .HasKey(p => p.UserId);
        builder.Entity<Trading.PositionEntity>()
            .HasKey(p => new { p.UserId, p.Symbol });
        builder.Entity<Trading.OrderEntity>()
            .HasKey(p => p.OrderId);
        builder.Entity<OrderEntity>()
            .Property(o => o.SubmittedUtc)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v));

        builder.Entity<OrderEntity>()
            .Property(o => o.FilledUtc)
            .HasConversion(
                v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : (long?)null,
                v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : (DateTimeOffset?)null);

        builder.Entity<OrderEntity>()
            .HasIndex(o => new { o.UserId, o.SubmittedUtc });
    }
}

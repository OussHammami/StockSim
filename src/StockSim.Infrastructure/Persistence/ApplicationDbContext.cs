using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockSim.Infrastructure.Persistence.Entities;
using StockSim.Infrastructure.Persistence.Identity;

namespace StockSim.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<PortfolioEntity> Portfolios => Set<PortfolioEntity>();
    public DbSet<PositionEntity> Positions => Set<PositionEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<PortfolioEntity>()
            .HasKey(p => p.UserId);
        builder.Entity<PositionEntity>()
            .HasKey(p => new { p.UserId, p.Symbol });
        builder.Entity<OrderEntity>()
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

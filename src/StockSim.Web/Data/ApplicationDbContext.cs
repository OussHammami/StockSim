using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace StockSim.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Trading.PortfolioEntity> Portfolios => Set<Trading.PortfolioEntity>();
    public DbSet<Trading.PositionEntity> Positions => Set<Trading.PositionEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Trading.PortfolioEntity>()
            .HasKey(p => p.UserId);
        builder.Entity<Trading.PositionEntity>()
            .HasKey(p => new { p.UserId, p.Symbol });
    }

}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StockSim.Infrastructure.Persistence.Portfolioing;

public sealed class PortfolioDbContextFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    public PortfolioDbContext CreateDbContext(string[] args)
    {
        var cs =
            Environment.GetEnvironmentVariable("ConnectionStrings__PortfolioDb")
            ?? "Host=localhost;Port=5433;Database=stocksim_portfolio;Username=stocksim;Password=stocksim;Include Error Detail=true";

        var builder = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseNpgsql(cs, o => o.MigrationsAssembly(typeof(PortfolioDbContext).Assembly.FullName));

        return new PortfolioDbContext(builder.Options);
    }
}

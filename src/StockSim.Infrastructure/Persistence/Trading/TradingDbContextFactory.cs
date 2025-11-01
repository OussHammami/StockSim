using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StockSim.Infrastructure.Persistence.Trading;

public sealed class TradingDbContextFactory : IDesignTimeDbContextFactory<TradingDbContext>
{
    public TradingDbContext CreateDbContext(string[] args)
    {
        var cs =
            Environment.GetEnvironmentVariable("ConnectionStrings__TradingDb")
            ?? "Host=localhost;Port=5432;Database=stocksim_trading;Username=stocksim;Password=stocksim;Include Error Detail=true";

        var builder = new DbContextOptionsBuilder<TradingDbContext>()
            .UseNpgsql(cs, o => o.MigrationsAssembly(typeof(TradingDbContext).Assembly.FullName));

        return new TradingDbContext(builder.Options);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StockSim.Infrastructure.Identity;

public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        // 1) env var wins (works in Docker/CI)
        // 2) fallback local default for dev
        var cs =
            Environment.GetEnvironmentVariable("ConnectionStrings__AuthDb")
            ?? "Host=localhost;Port=5433;Database=stocksim_auth;Username=stocksim;Password=stocksim;Include Error Detail=true";

        var opts = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(cs, b => b.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName))
            .Options;

        return new AuthDbContext(opts);
    }
}

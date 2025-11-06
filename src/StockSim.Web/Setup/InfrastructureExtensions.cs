using StockSim.Infrastructure;
using StockSim.Infrastructure.Identity;
using StockSim.Infrastructure.Persistence.Portfolioing;
using StockSim.Infrastructure.Persistence.Trading;
using StockSim.Web.Health;

namespace StockSim.Web.Setup;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddHealthChecksForApp(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<TradingDbContext>("trading-db", tags: new[] { "ready" })
            .AddDbContextCheck<PortfolioDbContext>("portfolio-db", tags: new[] { "ready" })
            .AddDbContextCheck<AuthDbContext>("auth-db", tags: new[] { "ready" })
            .AddCheck<RabbitHealthCheck>("rabbit", tags: new[] { "ready" });

        return services;
    }
}

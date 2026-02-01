using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockSim.Application.Abstractions.Inbox;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Orders;
using StockSim.Application.Portfolios;
using StockSim.Infrastructure.Identity;
using StockSim.Infrastructure.Inbox;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Outbox;
using StockSim.Infrastructure.Persistence.Portfolioing;
using StockSim.Infrastructure.Persistence.Trading;
using StockSim.Infrastructure.Repositories;

namespace StockSim.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        var cs = cfg.GetConnectionString("AuthDb")
                 ?? throw new InvalidOperationException("Missing AuthDb Connection.");

        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddDbContextPool<AuthDbContext>(o => o.UseNpgsql(cs));
        services.AddPooledDbContextFactory<AuthDbContext>(o => o.UseNpgsql(cs));

        // Fail-fast Rabbit config validation (prevents accidental defaults in prod-like setups)
        services.AddOptions<RabbitOptions>()
            .Bind(cfg.GetSection("Rabbit"))
            .ValidateDataAnnotations()
            .Validate(o =>
                !string.IsNullOrWhiteSpace(o.Host) &&
                !string.IsNullOrWhiteSpace(o.User) &&
                !string.IsNullOrWhiteSpace(o.Pass) &&
                !string.IsNullOrWhiteSpace(o.Queue),
                "Rabbit configuration is missing required values (Host/User/Pass/Queue).")
            .ValidateOnStart();

        services.AddSingleton<RabbitConnection>();

        return services;
    }

    public static IServiceCollection AddEfRepositories(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> tradingDb,
        Action<DbContextOptionsBuilder> portfolioDb)
    {
        services.AddDbContext<TradingDbContext>(tradingDb);
        services.AddDbContext<PortfolioDbContext>(portfolioDb);

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPortfolioRepository, PortfolioRepository>();

        services.AddScoped<IOutboxWriter<ITradingOutboxContext>, EfOutboxWriter<TradingDbContext, ITradingOutboxContext>>();
        services.AddScoped<IOutboxWriter<IPortfolioOutboxContext>, EfOutboxWriter<PortfolioDbContext, IPortfolioOutboxContext>>();

        services.AddScoped<IInboxStore<ITradingInboxContext>, EfInboxStore<TradingDbContext, ITradingInboxContext>>();
        services.AddScoped<IInboxStore<IPortfolioInboxContext>, EfInboxStore<PortfolioDbContext, IPortfolioInboxContext>>();

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockSim.Application.Abstractions;
using StockSim.Application.Integration;
using StockSim.Application.Orders;
using StockSim.Application.Portfolios;
using StockSim.Infrastructure.Inbox;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Outbox;
using StockSim.Infrastructure.Persistence;
using StockSim.Infrastructure.Persistence.Portfolioing;
using StockSim.Infrastructure.Persistence.Trading;
using StockSim.Infrastructure.Repositories;
using StockSim.Web.Services;

namespace StockSim.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        var cs = cfg.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Missing DefaultConnection.");
        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddDbContextPool<ApplicationDbContext>(o => o.UseNpgsql(cs));
        services.AddPooledDbContextFactory<ApplicationDbContext>(o => o.UseNpgsql(cs));


        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IOrderQueries, OrderQueries>();

        services.Configure<RabbitOptions>(cfg.GetSection("Rabbit"));
        services.AddSingleton<RabbitConnection>();
        services.AddSingleton<IOrderPublisher, OrderPublisher>();

        services.AddSingleton<IClock, SystemClock>();
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

        services.AddScoped<IOutboxWriter, EfOutboxWriter>();
        services.AddScoped<IInboxStore, EfInboxStore>();
        return services;
    }
}

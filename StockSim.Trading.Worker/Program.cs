using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StockSim.Application;
using StockSim.Infrastructure;

Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddApplicationCore();

        services.AddEfRepositories(
            tradingDb: o => o.UseNpgsql(ctx.Configuration.GetConnectionString("TradingDb")),
            portfolioDb: o => o.UseNpgsql(ctx.Configuration.GetConnectionString("PortfolioDb"))
        );

        services.AddHostedService<TradingOutboxDispatcher>();
        services.AddHostedService<HealthHost>();
    })
    .Build()
    .Run();

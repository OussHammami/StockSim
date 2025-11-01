using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StockSim.Application;
using StockSim.Application.Telemetry;
using StockSim.Infrastructure;

Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddApplicationCore();

        services.AddEfRepositories(
            tradingDb: o => o.UseNpgsql(ctx.Configuration.GetConnectionString("TradingDb")),
            portfolioDb: o => o.UseNpgsql(ctx.Configuration.GetConnectionString("PortfolioDb"))
        );

        services.AddHostedService<PortfolioOutboxDispatcher>();
        services.AddHostedService<HealthHost>();

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("stocksim.portfolio.worker"))
            .WithTracing(b => b
                .AddHttpClientInstrumentation()
                .AddSource(Telemetry.PortfolioSourceName)
                .AddConsoleExporter())
            .WithMetrics(b => b
                .AddRuntimeInstrumentation()
                .AddMeter(Telemetry.PortfolioSourceName)
                .AddPrometheusExporter());
    })
    .Build()
    .Run();

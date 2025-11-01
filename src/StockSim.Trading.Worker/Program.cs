using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StockSim.Application;
using StockSim.Application.Telemetry;
using StockSim.Infrastructure;
using OpenTelemetry.Metrics;

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

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("stocksim.trading.worker"))
            .WithTracing(b => b
                .AddHttpClientInstrumentation()
                .AddSource(Telemetry.OrdersSourceName)
                .AddConsoleExporter())
            .WithMetrics(b => b
                .AddRuntimeInstrumentation()
                .AddMeter(Telemetry.OrdersSourceName)
                .AddPrometheusExporter());
    })
    .Build()
    .Run();

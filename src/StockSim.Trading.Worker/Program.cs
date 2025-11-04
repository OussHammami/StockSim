using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StockSim.Application;
using StockSim.Application.Abstractions.Inbox;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Telemetry;
using StockSim.Infrastructure;
using StockSim.Infrastructure.Inbox;
using StockSim.Infrastructure.Outbox;
using StockSim.Infrastructure.Persistence.Portfolioing;
using StockSim.Infrastructure.Persistence.Trading;

Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddApplicationCore();

        services.AddEfRepositories(
            tradingDb: o => o.UseNpgsql(ctx.Configuration.GetConnectionString("TradingDb")),
            portfolioDb: o => o.UseNpgsql(ctx.Configuration.GetConnectionString("PortfolioDb"))
        );

        services.AddHostedService<TradingOutboxPublisher>();
        services.AddHostedService<HealthHost>();
        services.Configure<StockSim.Infrastructure.Messaging.RabbitOptions>(ctx.Configuration.GetSection("Rabbit"));
        services.AddSingleton<StockSim.Infrastructure.Messaging.RabbitConnection>();

        services.AddScoped<IOutboxWriter<ITradingOutboxContext>, EfOutboxWriter<TradingDbContext, ITradingOutboxContext>>();
        services.AddScoped<IInboxStore<ITradingInboxContext>, EfInboxStore<TradingDbContext, ITradingInboxContext>>();

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

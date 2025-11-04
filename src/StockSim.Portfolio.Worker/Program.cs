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

Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddApplicationCore();

        services.AddEfRepositories(
            tradingDb: o => o.UseNpgsql(ctx.Configuration.GetConnectionString("TradingDb")),
            portfolioDb: o => o.UseNpgsql(ctx.Configuration.GetConnectionString("PortfolioDb"))
        );

        services.AddHostedService<PortfolioOutboxPublisher>();
        services.AddHostedService<HealthHost>();
        services.Configure<StockSim.Infrastructure.Messaging.RabbitOptions>(ctx.Configuration.GetSection("Rabbit"));
        services.AddSingleton<StockSim.Infrastructure.Messaging.RabbitConnection>();
        services.AddScoped<StockSim.Portfolio.Worker.ITradingEventHandler, StockSim.Portfolio.Worker.DefaultTradingEventHandler>();
        services.AddHostedService<StockSim.Portfolio.Worker.TradingEventConsumer>();

        services.AddScoped<IOutboxWriter<IPortfolioOutboxContext>, EfOutboxWriter<PortfolioDbContext, IPortfolioOutboxContext>>();
        services.AddScoped<IInboxStore<IPortfolioInboxContext>, EfInboxStore<PortfolioDbContext, IPortfolioInboxContext>>();

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

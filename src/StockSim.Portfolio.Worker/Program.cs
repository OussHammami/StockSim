using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StockSim.Application;
using StockSim.Application.Abstractions.Inbox;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
using StockSim.Application.Portfolios;
using StockSim.Application.Telemetry;
using StockSim.Infrastructure.Inbox;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Outbox;
using StockSim.Infrastructure.Persistence.Portfolioing;
using StockSim.Infrastructure.Repositories;
using StockSim.Portfolio.Worker.External.Trading;
using System;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = Host.CreateApplicationBuilder(args);

// Load config files + env vars
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Log resolved connection string once for troubleshooting
var portfolioConn = builder.Configuration.GetConnectionString("PortfolioDb");
builder.Logging.AddConsole();

// Core application services
builder.Services.AddSingleton<IIntegrationEventMapper, DefaultIntegrationEventMapper>();

// ONLY PortfolioDb in this worker
builder.Services.AddDbContext<PortfolioDbContext>(opt =>  opt.UseNpgsql(portfolioConn));
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();


// RabbitMQ
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("Rabbit"));
builder.Services.AddSingleton<RabbitConnection>();

// Portfolio outbox publisher + health + consumer
builder.Services.AddHostedService<PortfolioOutboxPublisher>();
builder.Services.AddHostedService<HealthHost>();
builder.Services.AddHostedService<TradingEventsConsumer>();

// Inbox/Outbox bound to PortfolioDbContext
builder.Services.AddScoped<IOutboxWriter<IPortfolioOutboxContext>,
    EfOutboxWriter<PortfolioDbContext, IPortfolioOutboxContext>>();

builder.Services.AddScoped<IInboxStore<IPortfolioInboxContext>,
    EfInboxStore<PortfolioDbContext, IPortfolioInboxContext>>();

// Trading events handler into portfolio
builder.Services.AddScoped<ITradingEventHandler, TradingEventsHandler>(); 

builder.Services.AddHttpClient<IMarketPriceProvider, HttpMarketPriceProvider>(client =>
{
    var baseUrl = builder.Configuration.GetValue<string>("MarketFeed:BaseUrl") ?? "http://marketfeed:8081";
    client.BaseAddress = new Uri(baseUrl);
});

// Telemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("stocksim.portfolio.worker"))
    .WithTracing(b => b
        .AddSource(Telemetry.PortfolioSourceName)
        .AddHttpClientInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(b => b
        .AddMeter(Telemetry.PortfolioSourceName)
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

await builder.Build().RunAsync();

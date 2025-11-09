using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StockSim.Application;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Abstractions.Inbox;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Events;
using StockSim.Application.Integration;
using StockSim.Application.Orders;
using StockSim.Application.Orders.Execution;
using StockSim.Application.Telemetry;
using StockSim.Infrastructure.Inbox;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Outbox;
using StockSim.Infrastructure.Persistence.Trading;
using StockSim.Infrastructure.Repositories;
using StockSim.Trading.Worker.Dealer;
using StockSim.Trading.Worker.Execution;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = Host.CreateApplicationBuilder(args);

// Load config files + env vars
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Log resolved connection string once for troubleshooting
var tradingConn = builder.Configuration.GetConnectionString("TradingDb");
builder.Logging.AddConsole();

// Core application services
builder.Services.AddSingleton<IIntegrationEventMapper, DefaultIntegrationEventMapper>();

// ONLY TradingDb in this worker
builder.Services.AddDbContext<TradingDbContext>(opt =>
    opt.UseNpgsql(tradingConn));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// RabbitMQ
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("Rabbit"));
builder.Services.AddSingleton<RabbitConnection>();

// Trading outbox publisher + health + consumer
builder.Services.AddHostedService<TradingOutboxPublisher>();
builder.Services.AddHostedService<HealthHost>();

// Trading executor
builder.Services
    .AddSingleton<OrderBook>()
    .AddSingleton<ISlippageModel>(new LinearSlippageModel())
    .AddHostedService<OrderMaintenanceHostedService>()
    .AddHostedService<TapeDrivenExecutionHostedService>()
    .AddSingleton<ITradePrintStream, TapeDealerHostedService>()
    .AddSingleton<HubQuoteSnapshotProvider>()
    .AddSingleton<IQuoteSnapshotProvider>(sp => sp.GetRequiredService<HubQuoteSnapshotProvider>())
    .AddSingleton<IQuoteStream>(sp => sp.GetRequiredService<HubQuoteSnapshotProvider>())
    .AddScoped<TradePrintExecutor>()
    .AddScoped<IEventDispatcher, InContextEventDispatcher>()
    .AddHostedService<HubQuoteListenerHostedService>();

// Inbox/Outbox bound to TradingDbContext
builder.Services.AddScoped<IOutboxWriter<ITradingOutboxContext>,
    EfOutboxWriter<TradingDbContext, ITradingOutboxContext>>();

builder.Services.AddScoped<IInboxStore<ITradingInboxContext>,
    EfInboxStore<TradingDbContext, ITradingInboxContext>>();


// Telemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("stocksim.trading.worker"))
    .WithTracing(b => b
        .AddSource(Telemetry.OrdersSourceName)
        .AddHttpClientInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(b => b
        .AddMeter(Telemetry.OrdersSourceName)
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

await builder.Build().RunAsync();

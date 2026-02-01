using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Abstractions.Inbox;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Events;
using StockSim.Application.Integration;
using StockSim.Application.Options;
using StockSim.Application.Orders;
using StockSim.Application.Orders.Execution;
using StockSim.Application.Telemetry;
using StockSim.Infrastructure.Configuration;
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
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

builder.Configuration.AddEnvironmentVariables();

// Log resolved connection string once for troubleshooting
var tradingConn = builder.Configuration.GetRequiredConnectionString("TradingDb");
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

//MarketFeed
builder.Services.Configure<MarketFeedOptions>(builder.Configuration.GetSection("MarketFeed"));

// Trading outbox publisher + health + consumer
builder.Services.AddHostedService<TradingOutboxPublisher>();
builder.Services.AddHostedService<HealthHost>();

// Shared singletons
builder.Services
    .AddSingleton<OrderBook>()
    .AddSingleton<ISlippageModel>(new LinearSlippageModel())
    .AddSingleton<HubQuoteSnapshotProvider>()
    .AddSingleton<IQuoteSnapshotProvider>(sp => sp.GetRequiredService<HubQuoteSnapshotProvider>())
    .AddSingleton<IQuoteStream>(sp => sp.GetRequiredService<HubQuoteSnapshotProvider>())
    .AddSingleton<SymbolLocks>();

// Hosted services (ensure proper lifetimes)
builder.Services.AddHostedService<HubQuoteListenerHostedService>();

// Register TapeDealerHostedService ONCE and reuse it as both IHostedService and ITradePrintStream
builder.Services.AddSingleton<TapeDealerHostedService>();
builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<TapeDealerHostedService>());
builder.Services.AddSingleton<ITradePrintStream>(sp => sp.GetRequiredService<TapeDealerHostedService>());

// Scoped application services (resolved inside scopes by hosted services)
builder.Services
    .AddScoped<IEventDispatcher, InContextEventDispatcher>()
    .AddScoped<TradePrintExecutor>();

// Hosted services that create scopes for their work
builder.Services
    .AddHostedService<OrderMaintenanceHostedService>()
    .AddHostedService<TapeDrivenExecutionHostedService>();

// Inbox/Outbox bound to TradingDbContext
builder.Services.AddScoped<IOutboxWriter<ITradingOutboxContext>,
    EfOutboxWriter<TradingDbContext, ITradingOutboxContext>>();

builder.Services.AddScoped<IInboxStore<ITradingInboxContext>,
    EfInboxStore<TradingDbContext, ITradingInboxContext>>();


// Telemetry
var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
var serviceInstanceId = Environment.MachineName;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(
            serviceName: "stocksim.trading.worker",
            serviceNamespace: "stocksim",
            serviceVersion: serviceVersion,
            serviceInstanceId: serviceInstanceId)
        .AddAttributes(new[]
        {
            new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)
        }))
    .WithTracing(b => b
        .AddSource(Telemetry.OrdersSourceName)
        .AddEntityFrameworkCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(b => b
        .AddMeter(Telemetry.OrdersSourceName)
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());


await builder.Build().RunAsync();

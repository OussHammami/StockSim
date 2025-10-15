using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StockSim.Domain.Models;
using StockSim.MarketFeed.Hubs;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// CORS: allow the Blazor Web origin (update the port to your Web app HTTPS port)
const string AllowWeb = "_allowWeb";

builder.Host.UseSerilog((ctx, cfg) => cfg
    .Enrich.FromLogContext()
    .WriteTo.Console());

// OpenTelemetry metrics
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());


builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());



builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService(
        serviceName: "stocksim.web",
        serviceVersion: "1.0.0"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation(o =>
        {
            // do not trace Prometheus scrapes
            o.Filter = ctx => !(ctx.Request.Path.StartsWithSegments("/metrics")
                     || ctx.Request.Path.StartsWithSegments("/healthz")
                     || ctx.Request.Path.StartsWithSegments("/readyz"));
        })
        .AddHttpClientInstrumentation(
            o =>
            {
                o.EnrichWithHttpRequestMessage = (act, req) =>
                {
                    if (req.RequestUri?.Host == "marketfeed")
                        act?.SetTag("peer.service", "stocksim.marketfeed");
                };
            })
        .AddSource("StockSim.UI", "StockSim.Orders") 
        .AddZipkinExporter(o => o.Endpoint = new Uri("http://zipkin:9411/api/v2/spans")));

builder.Services.AddCors(o => o.AddPolicy(AllowWeb, p =>
    p.WithOrigins("https://localhost:7197", "http://localhost:8082")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// shared state via DI
builder.Services.AddSingleton(new ConcurrentDictionary<string, Quote>());
builder.Services.AddSingleton(new[] { "AAPL", "MSFT", "AMZN", "GOOGL", "NVDA", "TSLA", "META" });
builder.Services.AddHostedService<PriceWorker>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapPrometheusScrapingEndpoint("/metrics");

app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz");

app.UseHttpsRedirection();
app.UseCors(AllowWeb);
// list quotes
app.MapGet("/api/quotes", (ConcurrentDictionary<string, Quote> prices, string? symbolsCsv) =>
{
    var all = prices.Keys.ToArray();
    IEnumerable<string> req = string.IsNullOrWhiteSpace(symbolsCsv)
        ? all
        : symbolsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    return req.Select(s => prices.TryGetValue(s, out var q)
        ? q : new Quote(s, 0m, 0m, DateTimeOffset.UtcNow));
}).RequireCors(AllowWeb);

app.MapHub<QuoteHub>("/hubs/quotes").RequireCors(AllowWeb); ;
app.Run();

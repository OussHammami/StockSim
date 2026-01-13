using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Net.Http.Headers;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StockSim.Domain.MarketFeed;
using StockSim.MarketFeed.Hubs;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

const string CorsPolicy = "SignalRStrict";
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

// logging
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// OpenTelemetry consolidated
builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService(serviceName: "stocksim.marketfeed", serviceVersion: "1.0.0"))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter())
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation(o =>
        {
            // do not trace Prometheus and health scrapes
            o.Filter = ctx => !(ctx.Request.Path.StartsWithSegments("/metrics")
                     || ctx.Request.Path.StartsWithSegments("/healthz")
                     || ctx.Request.Path.StartsWithSegments("/readyz"));
        })
        .AddHttpClientInstrumentation(o =>
        {
            o.EnrichWithHttpRequestMessage = (act, req) =>
            {
                if (req.RequestUri?.Host == "marketfeed")
                    act?.SetTag("peer.service", "stocksim.marketfeed");
            };
        })
        .AddSource("StockSim.UI", "StockSim.Orders")
        .AddOtlpExporter());

builder.Services.AddCors(o =>
{
    o.AddPolicy(CorsPolicy, p =>
    {
        p.WithOrigins(origins)
         .WithHeaders(HeaderNames.ContentType, HeaderNames.Authorization, "x-requested-with")
         .WithMethods("GET", "POST", "OPTIONS")
         .AllowCredentials();
    });
});

// shared state via DI
builder.Services.AddSingleton(new ConcurrentDictionary<string, decimal>());
builder.Services.AddSingleton(new[] { "AAPL", "MSFT", "AMZN", "GOOGL", "NVDA", "TSLA", "META" });
builder.Services.AddHostedService<PriceWorker>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz");

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
});

app.UseHttpsRedirection();
app.Use(async (ctx, next) =>
{
    var connect = new List<string> { "'self'" };
    foreach (var o in origins)
    {
        connect.Add(o);
        if (o.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            connect.Add("wss://" + o.Substring("https://".Length));
        if (o.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            connect.Add("ws://" + o.Substring("http://".Length));
    }

    var csp =
        $"default-src 'self'; " +
        $"base-uri 'self'; " +
        $"frame-ancestors 'none'; " +
        $"img-src 'self' data:; " +
        $"font-src 'self' data:; " +
        $"style-src 'self' 'unsafe-inline'; " +
        $"script-src 'self' 'unsafe-inline'; " +
        $"connect-src {string.Join(' ', connect)}";

    ctx.Response.Headers["Content-Security-Policy"] = csp;
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    await next();
});
app.UseCors(CorsPolicy);
// list quotes
app.MapGet("/api/quotes", (ConcurrentDictionary<string, decimal> prices, string? symbolsCsv) =>
{
    var all = prices.Keys.ToArray();
    IEnumerable<string> req = string.IsNullOrWhiteSpace(symbolsCsv)
        ? all
        : symbolsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    return req.Select<string, object>(s => prices.TryGetValue(s, out var q)
        ? q : new Quote(s, 0m, 0m, 0m, DateTimeOffset.UtcNow));
}).RequireCors(CorsPolicy);

app.MapHub<QuoteHub>("/hubs/quotes").RequireCors(CorsPolicy);
app.Run();

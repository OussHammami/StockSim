using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Persistence;
using StockSim.Web;
using StockSim.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// services
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .Enrich.FromLogContext()
    .WriteTo.Console());

// OpenTelemetry metrics
builder.Services.AddOpenTelemetry()
  .ConfigureResource(r => r.AddService("stocksim.web", serviceVersion: "1.0.0"))
  .WithMetrics(m => m
      .AddAspNetCoreInstrumentation()
      .AddHttpClientInstrumentation()
      .AddRuntimeInstrumentation()
      .AddProcessInstrumentation()
      .AddPrometheusExporter())
  .WithTracing(t => t
      .AddAspNetCoreInstrumentation(o =>
          o.Filter = ctx => !(ctx.Request.Path.StartsWithSegments("/metrics")
                           || ctx.Request.Path.StartsWithSegments("/healthz")
                           || ctx.Request.Path.StartsWithSegments("/readyz")))
      .AddHttpClientInstrumentation(o =>
      {
          o.EnrichWithHttpRequestMessage = (act, req) =>
          {
              if (req.RequestUri?.Host == "marketfeed")
                  act?.SetTag("peer.service", "stocksim.marketfeed");
          };
      })
      .AddSource("StockSim.UI", "StockSim.Orders")
      .AddZipkinExporter(o => o.Endpoint = new Uri("http://zipkin:9411/api/v2/spans")));

builder.Services.AddAppIdentity(builder.Configuration);
builder.Services.AddDomainServices(builder.Configuration);
builder.Services.AddUiServices();
builder.Services.Configure<CircuitOptions>(builder.Configuration.GetSection("CircuitOptions"));

var app = builder.Build();

app.UseSerilogRequestLogging();
app.MapPrometheusScrapingEndpoint("/metrics");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

// pipeline
app.UseAppPipeline();

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.MapHub<OrderHub>("/hubs/orders");

app.Run();

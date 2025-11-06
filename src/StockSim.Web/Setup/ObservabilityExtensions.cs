using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

namespace StockSim.Web.Setup;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, cfg) => cfg.Enrich.FromLogContext().WriteTo.Console());

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("stocksim.web", serviceVersion: "1.0.0"))
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter("StockSim.Orders", "StockSim.Portfolio")
                .AddOtlpExporter()
                .AddPrometheusExporter())
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation(o =>
                    o.Filter = ctx => !(ctx.Request.Path.StartsWithSegments("/metrics")
                                     || ctx.Request.Path.StartsWithSegments("/healthz")
                                     || ctx.Request.Path.StartsWithSegments("/readyz")))
                .AddHttpClientInstrumentation()
                .AddSource("StockSim.UI", "StockSim.Orders", "StockSim.Portfolio")
                .AddZipkinExporter(o => o.Endpoint = new Uri("http://zipkin:9411/api/v2/spans"))
                .AddOtlpExporter());

        return builder;
    }
}

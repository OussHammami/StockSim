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

        var serviceVersion = typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "unknown";
        var serviceInstanceId = Environment.MachineName;

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(
                    serviceName: "stocksim.web",
                    serviceNamespace: "stocksim",
                    serviceVersion: serviceVersion,
                    serviceInstanceId: serviceInstanceId)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)
                }))
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("StockSim.Orders", "StockSim.Portfolio")
                .AddOtlpExporter())
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation(o =>
                    o.Filter = ctx => !(ctx.Request.Path.StartsWithSegments("/metrics")
                                     || ctx.Request.Path.StartsWithSegments("/healthz")
                                     || ctx.Request.Path.StartsWithSegments("/readyz")))
                .AddEntityFrameworkCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("StockSim.UI", "StockSim.Orders", "StockSim.Portfolio")
                .AddOtlpExporter());

        return builder;
    }
}

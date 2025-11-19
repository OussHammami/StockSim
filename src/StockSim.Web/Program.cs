using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StockSim.Application;
using StockSim.Infrastructure;
using StockSim.Infrastructure.Identity;
using StockSim.Infrastructure.Persistence.Portfolioing;
using StockSim.Infrastructure.Persistence.Trading;
using StockSim.Web.Components;
using StockSim.Web.Demo;
using StockSim.Web.Hubs;
using StockSim.Web.Setup;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. cross-cutting
builder.AddObservability();

// 2. core app services
builder.Services
    .AddAppIdentity(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddHealthChecksForApp()
    .AddUi(builder.Configuration, builder.Environment)
    .AddSecurity(builder.Environment)
    .AddApplicationCore();

// 3. dbs + repos
builder.Services.AddEfRepositories(
    tradingDb: o =>
    {
        o.UseNpgsql(builder.Configuration.GetConnectionString("TradingDb"));
        if (builder.Environment.IsDevelopment())
        {
            o.EnableDetailedErrors()
             .EnableSensitiveDataLogging()
             .LogTo(Console.WriteLine, LogLevel.Information);
        }
    },
    portfolioDb: o =>
    {
        o.UseNpgsql(builder.Configuration.GetConnectionString("PortfolioDb"));
        if (builder.Environment.IsDevelopment())
        {
            o.EnableDetailedErrors()
             .EnableSensitiveDataLogging()
             .LogTo(Console.WriteLine, LogLevel.Information);
        }
    });

// 4. mvc + json + validation
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// 5. CORS
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
const string CorsPolicy = "SignalRStrict";
builder.Services.AddCorsForSignalR(CorsPolicy, origins);

// 6. problem details
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        var traceId = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions["traceId"] = traceId;

        if (builder.Environment.IsDevelopment() || builder.Environment.EnvironmentName == "Testing")
        {
            var ex = ctx.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
            if (ex is not null)
            {
                ctx.ProblemDetails.Extensions["exception"] = ex.Message;
                ctx.ProblemDetails.Extensions["stackTrace"] = ex.StackTrace;
            }
        }
    };
});

// 7. demo seed
builder.Services.Configure<DemoSeedOptions>(builder.Configuration.GetSection("DemoSeed"));
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DemoSeedHostedService>();
    builder.Services.AddHostedService<IdentitySeedHostedService>();
}

var app = builder.Build();

// AUTO-APPLY EF CORE MIGRATIONS ON STARTUP (Trading + Portfolio)
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbMigration");

    try
    {
        var tradingDb = sp.GetRequiredService<TradingDbContext>();
        await tradingDb.Database.MigrateAsync();

        var portfolioDb = sp.GetRequiredService<PortfolioDbContext>();
        await portfolioDb.Database.MigrateAsync();

        var authDb = sp.GetRequiredService<AuthDbContext>();
        await authDb.Database.MigrateAsync();

        logger.LogInformation("Applied database migrations for TradingDb, PortfolioDb and AuthDb.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error applying database migrations.");
        throw;
    }
}

// pipeline
app.UseBasePipeline();

app.UseCors(CorsPolicy);
app.UseSecurityHeaders(origins);

// endpoints
app.MapUiThemeEndpoint();
app.MapControllers();
app.MapHub<QuotesHub>("/hubs/quotes");
app.MapAppEndpoints<App, OrderHub>(CorsPolicy);

// test only
if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/test/csp", () => Results.Ok("ok")).AllowAnonymous();
}

// dev-only admin
if (app.Environment.IsDevelopment())
{
    app.MapDemoReset();
}

app.Run();

namespace StockSim.Web
{
    public partial class Program { }
}

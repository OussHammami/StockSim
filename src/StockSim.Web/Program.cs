using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StockSim.Application;
using StockSim.Infrastructure;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Persistence;
using StockSim.Web;
using StockSim.Web.Components;
using StockSim.Web.Demo;
using StockSim.Web.Hubs;
using StockSim.Web.Options;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
const string CorsPolicy = "SignalRStrict";
// Observability (Serilog + OTel)
builder.AddObservability();

// App services
builder.Services.AddAppIdentity(builder.Configuration);

builder.Services.AddDomainServices(builder.Configuration);
builder.Services.AddCors(o =>
{
    o.AddPolicy(CorsPolicy, p =>
    {
        p.WithOrigins(origins)
        .WithHeaders(Microsoft.Net.Http.Headers.HeaderNames.ContentType, Microsoft.Net.Http.Headers.HeaderNames.Authorization, "x-requested-with")
        .WithMethods("GET", "POST", "OPTIONS")
        .AllowCredentials();
    });
});
builder.Services.AddUiServices(builder.Configuration, builder.Environment);
builder.Services.AddSecurity(builder.Environment);

builder.Services.AddApplicationCore();
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
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        // always include a trace id
        var traceId = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
        ctx.ProblemDetails.Extensions["traceId"] = traceId;

        // add exception details only in Development
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

builder.Services.Configure<DemoSeedOptions>(builder.Configuration.GetSection("DemoSeed"));


if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DemoSeedHostedService>();
    builder.Services.AddHostedService<IdentitySeedHostedService>();    
}

var app = builder.Build();

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/test/csp", () => Results.Ok("ok")).AllowAnonymous();
}
// Pipeline + endpoints
app.UseRequestPipeline();
app.UseCors(CorsPolicy);
app.UseSecurityHeaders(origins);
app.MapGet("/ui/theme", (bool dark, HttpContext ctx) =>
{
    ctx.Response.Cookies.Append(
        "stocksim_theme",
        dark ? "dark" : "light",
        new CookieOptions {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            Secure = ctx.Request.IsHttps
        });

    var referer = ctx.Request.Headers.Referer.ToString();
    return Results.Redirect(string.IsNullOrEmpty(referer) ? "/" : referer);
}).AllowAnonymous();
app.UseAntiforgery();
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async ctx =>
    {
        var feat = ctx.Features.Get<IExceptionHandlerFeature>();
        var ex = feat?.Error;

        var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Unhandled");
        var traceId = Activity.Current?.Id ?? ctx.TraceIdentifier;

        logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", traceId);

        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "An error occurred while processing your request.",
            Status = 500,
            Extensions = { ["traceId"] = traceId }
        });
    });
});
app.UseStatusCodePages();
app.MapControllers();
app.MapHub<QuotesHub>("/hubs/quotes");
app.MapAppEndpoints<App, OrderHub>(CorsPolicy);
if (builder.Environment.IsDevelopment())
{   
    app.MapPost("/admin/reset-demo", async (
        StockSim.Infrastructure.Persistence.Trading.TradingDbContext tdb,
        StockSim.Infrastructure.Persistence.Portfolioing.PortfolioDbContext pdb) =>
    {
        // stop workers during reset if they run in the same process
        await tdb.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE "orders","outbox_messages" RESTART IDENTITY CASCADE;
            """);
        await pdb.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE "portfolios","positions","inbox_messages" RESTART IDENTITY CASCADE;
            """);
        return Results.Ok();
    })
    .RequireAuthorization(p => p.RequireRole("Admin"));
}
app.Run();

namespace StockSim.Web
{
    public partial class Program { }
}

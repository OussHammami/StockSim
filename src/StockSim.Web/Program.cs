using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockSim.Application;
using StockSim.Infrastructure;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Persistence;
using StockSim.Web;
using StockSim.Web.Components;
using StockSim.Web.Hubs;
using System.Diagnostics;

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
builder.Services.AddControllers().AddJsonOptions(o => { /* keep defaults */ });
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
        if (builder.Environment.IsDevelopment())
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


var app = builder.Build();

// DB migrate
app.ApplyMigrations<ApplicationDbContext>();

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
app.MapPost("/admin/reset-demo", async (ApplicationDbContext db) =>
{
    await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Orders\",\"Positions\",\"Portfolios\",\"OutboxMessages\" RESTART IDENTITY CASCADE;");
    return Results.Ok();
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

app.Run();

namespace StockSim.Web
{
    public partial class Program { }
}

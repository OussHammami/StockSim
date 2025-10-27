using Microsoft.EntityFrameworkCore;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Persistence;
using StockSim.Web;
using StockSim.Web.Components;
using StockSim.Web.Hubs;

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
builder.Services.AddUiServices();
builder.Services.AddSecurity(builder.Environment);

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

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StockSim.Web.Components;
using StockSim.Web.Hubs;

namespace StockSim.Web.Setup;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapAppEndpoints<TRoot, THub>(this IEndpointRouteBuilder app, string corsPolicy)
        where TRoot : class
        where THub : Hub
    {
        app.MapHealthChecks("/healthz");
        app.MapHealthChecks("/readyz", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });
        app.MapHub<THub>("/hubs/orders").RequireCors(corsPolicy);
        app.MapGet("/", (HttpContext ctx) =>
        {
            var to = "/dashboard";
            if (ctx.User.Identity?.IsAuthenticated == true)
                return Results.Redirect(to);

            var ru = Uri.EscapeDataString(to);
            return Results.Redirect($"/Account/Login?returnUrl={ru}");
        });
        app.MapRazorComponents<TRoot>().AddInteractiveServerRenderMode();
        app.MapAdditionalIdentityEndpoints();
        return app;
    }

    public static IEndpointRouteBuilder MapUiThemeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/ui/theme", (bool dark, HttpContext ctx) =>
        {
            ctx.Response.Cookies.Append(
                "stocksim_theme",
                dark ? "dark" : "light",
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    HttpOnly = false,
                    SameSite = SameSiteMode.Lax,
                    Secure = ctx.Request.IsHttps
                });

            var referer = ctx.Request.Headers.Referer.ToString();
            return Results.Redirect(string.IsNullOrEmpty(referer) ? "/" : referer);
        }).AllowAnonymous();

        return app;
    }

    public static IEndpointRouteBuilder MapDemoReset(this IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/reset-demo", async (
            StockSim.Infrastructure.Persistence.Trading.TradingDbContext tdb,
            StockSim.Infrastructure.Persistence.Portfolioing.PortfolioDbContext pdb) =>
        {
            await tdb.Database.ExecuteSqlRawAsync("""
                TRUNCATE TABLE "orders","outbox_messages" RESTART IDENTITY CASCADE;
                """);
            await pdb.Database.ExecuteSqlRawAsync("""
                TRUNCATE TABLE "portfolios","positions","inbox_messages" RESTART IDENTITY CASCADE;
                """);
            return Results.Ok();
        })
        .RequireAuthorization(p => p.RequireRole("Admin"));

        return app;
    }

    public static IServiceCollection AddCorsForSignalR(this IServiceCollection services, string policyName, string[] origins)
    {
        services.AddCors(o =>
        {
            o.AddPolicy(policyName, p =>
            {
                p.WithOrigins(origins)
                 .WithHeaders(
                     Microsoft.Net.Http.Headers.HeaderNames.ContentType,
                     Microsoft.Net.Http.Headers.HeaderNames.Authorization,
                     "x-requested-with")
                 .WithMethods("GET", "POST", "OPTIONS")
                 .AllowCredentials();
            });
        });

        return services;
    }
}

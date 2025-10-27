using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using StockSim.Infrastructure;
using StockSim.Infrastructure.Persistence;
using StockSim.Infrastructure.Persistence.Identity;
using StockSim.Web.Components.Account;
using StockSim.Web.Health;
using StockSim.Web.Hubs;
using StockSim.Web.Services;

namespace StockSim.Web;

public static class StartupExtensions
{
    // ---------- Services ----------

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, cfg) => cfg.Enrich.FromLogContext().WriteTo.Console());

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("stocksim.web", serviceVersion: "1.0.0"))
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter()
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
                .AddZipkinExporter(o => o.Endpoint = new Uri("http://zipkin:9411/api/v2/spans"))
                .AddOtlpExporter());
        return builder;
    }

    public static IServiceCollection AddAppIdentity(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddCascadingAuthenticationState();
        services.AddScoped<IdentityUserAccessor>();
        services.AddScoped<IdentityRedirectManager>();
        services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        services.AddAuthentication(o =>
        {
            o.DefaultScheme = IdentityConstants.ApplicationScheme;
            o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        }).AddIdentityCookies();

        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddIdentityCore<ApplicationUser>(o =>
        {
            o.SignIn.RequireConfirmedAccount = false;
            o.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();
        
        services.AddAuthorization();
        services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
        return services;
    }

    public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddInfrastructure(cfg);
        services.Configure<DemoOptions>(cfg.GetSection("DEMO"));
        services.AddHostedService<DemoSeedHostedService>();
        services.AddScoped<HubStatusService>();
        services.AddHostedService<OrderConsumer>();
        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<QuoteMatcherService>();
        services.AddSingleton<LastQuotesCache>();

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("db", tags: new[] { "ready" })
            .AddCheck<RabbitHealthCheck>("rabbit", tags: new[] { "ready" });

        services.AddHttpClient("MarketFeed", (sp, client) =>
        {
            var baseUrl = cfg["MarketFeed:BaseUrl"] ?? "http://localhost:8081";
            client.BaseAddress = new Uri(baseUrl);
        });

        return services;
    }

    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {        
        services.AddMudServices(o =>
        {
            o.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
            o.SnackbarConfiguration.VisibleStateDuration = 2500;
        });
        services.AddSignalR();
        services.AddRazorComponents().AddInteractiveServerComponents(o => o.DetailedErrors = true);
        services.AddSingleton<IThemePrefService, ThemePrefService>();
        services.AddHttpContextAccessor();

        return services;
    }

    public static IServiceCollection AddSecurity(this IServiceCollection services, IWebHostEnvironment env)
    {
        services.ConfigureApplicationCookie(o =>
        {
            o.Cookie.Name = ".stocksim.auth";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = env.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
            o.SlidingExpiration = true;
            o.ExpireTimeSpan = TimeSpan.FromHours(8);
            o.LoginPath = "/Account/Login";
            o.AccessDeniedPath = "/Account/AccessDenied";
        });

        services.AddAntiforgery(o =>
        {
            o.Cookie.Name = ".stocksim.af";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Strict;
            o.Cookie.SecurePolicy = env.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
        });

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddFixedWindowLimiter("global", opt =>
            {
                opt.PermitLimit = 300;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });
        });
        return services;
    }

    // ---------- Pipeline + endpoints ----------

    public static WebApplication UseRequestPipeline(this WebApplication app)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }
        
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseSerilogRequestLogging();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        return app;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, string[] origins)
        => app.Use(async (ctx, next) =>
        {
            // base CSP
            // connect-src includes self, explicit origins, and ws/wss for those origins
            var connect = new List<string> { "'self'" };
            foreach (var o in origins)
            {
                connect.Add(o);
                // map http(s) origin to ws(s) for SignalR
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
                $"style-src 'self' 'unsafe-inline'; " + // Blazor/Prerender styles
                $"script-src 'self' 'unsafe-inline'; " + // Blazor Server boot script
                $"connect-src {string.Join(' ', connect)}";

            ctx.Response.Headers["Content-Security-Policy"] = csp;
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
            ctx.Response.Headers["X-Frame-Options"] = "DENY";
            await next();
        });


    public static IEndpointRouteBuilder MapAppEndpoints<TRoot, THub>(this IEndpointRouteBuilder app, string corsPolicy)
        where TRoot : class
        where THub : Hub
    {
        app.MapPrometheusScrapingEndpoint("/metrics");
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

    // ---------- Utilities ----------

    public static WebApplication ApplyMigrations<TContext>(this WebApplication app) where TContext : DbContext
    {
        if (app.Environment.IsEnvironment("Testing"))
            return app;
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        db.Database.Migrate();
        return app;
    }
}

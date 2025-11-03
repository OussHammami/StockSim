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
using StockSim.Application.MarketData.Feed;
using StockSim.Infrastructure;
using StockSim.Infrastructure.Identity;
using StockSim.Infrastructure.Persistence;
using StockSim.Infrastructure.Persistence.Portfolioing;
using StockSim.Infrastructure.Persistence.Trading;
using StockSim.Web.Components.Account;
using StockSim.Web.Health;
using StockSim.Web.Http;
using StockSim.Web.Hubs;
using StockSim.Web.Options;
using StockSim.Web.Services;
using System.Diagnostics;

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
                .AddMeter("StockSim.Orders", "StockSim.Portfolio")
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
                .AddSource("StockSim.UI", "StockSim.Orders", "StockSim.Portfolio")
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
        .AddEntityFrameworkStores<AuthDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();
        
        services.AddAuthorization();
        services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
        return services;
    }

    public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddInfrastructure(cfg);

        services.AddHealthChecks()
            .AddDbContextCheck<TradingDbContext>("trading-db", tags: new[] { "ready" })
            .AddDbContextCheck<PortfolioDbContext>("portfolio-db", tags: new[] { "ready" })
            .AddDbContextCheck<AuthDbContext>("auth-db", tags: new[] { "ready" })
            .AddCheck<RabbitHealthCheck>("rabbit", tags: new[] { "ready" });

        services.AddHttpClient("MarketFeed", (sp, client) =>
        {
            var baseUrl = cfg["MarketFeed:BaseUrl"] ?? "http://localhost:8081";
            client.BaseAddress = new Uri(baseUrl);
        });

        return services;
    }

    public static IServiceCollection AddUiServices(this IServiceCollection services, ConfigurationManager configuration, IWebHostEnvironment environment)
    {
        services.Configure<MarketFeedOptions>(configuration.GetSection("MarketFeed"));
        services.AddSingleton<QuotesHubClient>();
        services.AddMudServices(o =>
        {
            o.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
            o.SnackbarConfiguration.VisibleStateDuration = 2500;
        });
        services.AddSignalR(o =>
        {
            o.EnableDetailedErrors = environment.IsDevelopment();
        });
        services.AddSingleton<IMarketDataFeed, FakeMarketDataFeed>();

        services.AddHttpContextAccessor();
        services.AddTransient<ForwardAuthHeadersHandler>();

        services.AddHttpClient<TradingClient>((sp, c) =>
        {
            var ctx = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
            var baseUri = new Uri($"{ctx!.Request.Scheme}://{ctx.Request.Host}{ctx.Request.PathBase}");
            c.BaseAddress = baseUri;
        }).AddHttpMessageHandler<ForwardAuthHeadersHandler>();
        services.AddHttpClient<PortfolioClient>((sp, c) =>
        {
            var ctx = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
            var baseUri = new Uri($"{ctx!.Request.Scheme}://{ctx.Request.Host}{ctx.Request.PathBase}");
            c.BaseAddress = baseUri;
        }).AddHttpMessageHandler<ForwardAuthHeadersHandler>();
        services.AddHostedService<MarketDataStreamer>();
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
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }
        
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseSerilogRequestLogging(opts =>
        {
            opts.EnrichDiagnosticContext = (dc, http) =>
            {
                dc.Set("UserId", http.User?.Identity?.Name);
                dc.Set("TraceId", Activity.Current?.Id ?? http.TraceIdentifier);
                dc.Set("Path", http.Request.Path);
            };
        });
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/healthz", () => Results.Ok("ok")).AllowAnonymous();
        app.MapGet("/readyz", () => Results.Ok("ready")).AllowAnonymous();
        return app;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, string[] origins)
        => app.Use(async (ctx, next) =>
        {
            var env = ctx.RequestServices.GetRequiredService<IHostEnvironment>();

            var connect = new List<string> { "'self'" };
            foreach (var o in origins)
            {
                connect.Add(o);
                if (o.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    connect.Add("wss://" + o.Substring("https://".Length));
                if (o.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    connect.Add("ws://" + o.Substring("http://".Length));
            }

            // Loosen in Development: allow any localhost port and dev websockets
            if (env.IsDevelopment())
            {
                connect.AddRange(new[]
                {
                    "http://localhost:*",
                    "ws://localhost:*",
                    "wss://localhost:*"
                });
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
            var traceId = Activity.Current?.Id ?? ctx.TraceIdentifier;
            ctx.Response.Headers["X-TraceId"] = traceId;

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

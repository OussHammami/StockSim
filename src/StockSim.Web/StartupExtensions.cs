using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using MudBlazor;
using MudBlazor.Services;
using StockSim.Infrastructure;
using StockSim.Infrastructure.Persistence;
using StockSim.Infrastructure.Persistence.Identity;
using StockSim.Web.Components.Account;
using StockSim.Web.Health;
using StockSim.Web.Services;

namespace StockSim.Web;

public static class StartupExtensions
{
    // Identity + EF + auth cookies
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
        })
        .AddIdentityCookies();


        services.AddDatabaseDeveloperPageExceptionFilter();

        services.AddIdentityCore<ApplicationUser>(o =>
        {
            o.SignIn.RequireConfirmedAccount = false;
            o.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
        return services;
    }

    // Domain services: portfolio, RabbitMQ, quotes cache, MarketFeed client
    public static IServiceCollection AddDomainServices(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddInfrastructure(cfg);
        services.AddSingleton<LastQuotesCache>();
        services.AddHostedService<OrderConsumer>();
        services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("db", tags: new[] { "ready" })
        .AddCheck<RabbitHealthCheck>("rabbit", tags: new[] { "ready" });

        services.AddHttpClient("MarketFeed", (sp, client) =>
        {
            var baseUrl = cfg["MarketFeed:BaseUrl"] ?? "https://localhost:7173";
            client.BaseAddress = new Uri(baseUrl);
        });

        return services;
    }

    // UI framework
    public static IServiceCollection AddUiServices(this IServiceCollection services)
    {
        services.AddMudServices(o =>
        {
            o.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            o.SnackbarConfiguration.VisibleStateDuration = 2500;
        });
        services.AddSignalR();

        return services;
    }

    // Middleware pipeline
    public static WebApplication UseAppPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
            app.UseHttpsRedirection();
        }
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseStaticFiles();
        app.UseAntiforgery();
        return app;
    }
}

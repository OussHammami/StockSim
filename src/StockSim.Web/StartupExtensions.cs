using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;
using StockSim.Web.Components.Account;
using StockSim.Web.Data;
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

        var cs = cfg.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Missing DefaultConnection.");

        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(cs));
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
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IOrderService, OrderService>();

        services.AddSingleton<LastQuotesCache>();

        services.Configure<RabbitOptions>(cfg.GetSection("Rabbit"));
        services.AddSingleton<RabbitConnection>();
        services.AddSingleton<IOrderPublisher, OrderPublisher>();
        services.AddHostedService<OrderConsumer>();

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
        }
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();
        return app;
    }
}

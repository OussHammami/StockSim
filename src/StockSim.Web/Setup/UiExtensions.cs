using MudBlazor.Services;
using StockSim.Application.MarketData.Feed;
using StockSim.Application.Options;
using StockSim.Web.Http;
using StockSim.Web.Services;

namespace StockSim.Web.Setup;

public static class UiExtensions
{
    public static IServiceCollection AddUi(this IServiceCollection services, ConfigurationManager configuration, IWebHostEnvironment environment)
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

        static Uri ResolveBaseUri(IServiceProvider sp, IConfiguration configuration)
        {
            static Uri EnsureTrailingSlash(Uri uri)
                => uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");

            var internalBaseUrl = configuration["StockSim:InternalBaseUrl"];
            if (!string.IsNullOrWhiteSpace(internalBaseUrl) && Uri.TryCreate(internalBaseUrl, UriKind.Absolute, out var internalBaseUri))
            {
                var ctx = sp.GetService<IHttpContextAccessor>()?.HttpContext;
                var pathBase = ctx?.Request.PathBase.Value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(pathBase))
                    return EnsureTrailingSlash(internalBaseUri);

                var combined = new Uri(EnsureTrailingSlash(internalBaseUri), pathBase.TrimStart('/') + "/");
                return EnsureTrailingSlash(combined);
            }

            var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext
                ?? throw new InvalidOperationException("No HttpContext available to build HTTP client base address. Configure StockSim:InternalBaseUrl for non-request usage.");

            var fromRequest = new Uri($"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.PathBase}/");
            return fromRequest;
        }

        services.AddHttpClient<TradingClient>((sp, c) =>
        {
            c.BaseAddress = ResolveBaseUri(sp, configuration);
        }).AddHttpMessageHandler<ForwardAuthHeadersHandler>();

        services.AddHttpClient<PortfolioClient>((sp, c) =>
        {
            c.BaseAddress = ResolveBaseUri(sp, configuration);
        }).AddHttpMessageHandler<ForwardAuthHeadersHandler>();

        services.AddHostedService<MarketDataStreamer>();
        services.AddRazorComponents().AddInteractiveServerComponents(o => o.DetailedErrors = true);
        services.AddSingleton<IThemePrefService, ThemePrefService>();

        return services;
    }
}

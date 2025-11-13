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

        services.AddHttpClient<TradingClient>((sp, c) =>
        {
            var ctx = sp.GetRequiredService<IHttpContextAccessor>().HttpContext!;
            var baseUri = new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.PathBase}");
            c.BaseAddress = baseUri;
        }).AddHttpMessageHandler<ForwardAuthHeadersHandler>();

        services.AddHttpClient<PortfolioClient>((sp, c) =>
        {
            var ctx = sp.GetRequiredService<IHttpContextAccessor>().HttpContext!;
            var baseUri = new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.PathBase}");
            c.BaseAddress = baseUri;
        }).AddHttpMessageHandler<ForwardAuthHeadersHandler>();

        services.AddHostedService<MarketDataStreamer>();
        services.AddRazorComponents().AddInteractiveServerComponents(o => o.DetailedErrors = true);
        services.AddSingleton<IThemePrefService, ThemePrefService>();

        return services;
    }
}

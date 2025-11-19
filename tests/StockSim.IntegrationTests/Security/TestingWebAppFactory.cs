using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockSim.Infrastructure.Identity;
using StockSim.Infrastructure.Persistence.Portfolioing;
using StockSim.Infrastructure.Persistence.Trading;
using StockSim.Web;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace StockSim.IntegrationTests;

public sealed class TestingWebAppFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _tradingConn;
    private SqliteConnection? _portfolioConn;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(cfg =>
        {
            // Optional: flip any feature flags here
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoSeed:Enabled"] = "false",       // no demo seed during tests
                ["SecurityHeaders:Enabled"] = "false" // if you gate CSP by config
            });
        });

        builder.ConfigureServices(async services =>
        {
            // Remove real AuthDbContext and use InMemory
            RemoveDb<AuthDbContext>(services);
            services.AddDbContext<AuthDbContext>(o => o.UseInMemoryDatabase("stocksim-auth-tests"));

            // Replace DDD DbContexts with SQLite in-memory (relational behavior)
            RemoveDb<TradingDbContext>(services);
            RemoveDb<PortfolioDbContext>(services);

            _tradingConn = new SqliteConnection("Filename=:memory:");
            _tradingConn.Open();
            _portfolioConn = new SqliteConnection("Filename=:memory:");
            _portfolioConn.Open();

            services.AddDbContext<TradingDbContext>(o => o.UseSqlite(_tradingConn));
            services.AddDbContext<PortfolioDbContext>(o => o.UseSqlite(_portfolioConn));

            // Drop background hosted services from Web (consumers, dispatchers, etc.)
            var hosted = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                            d.ImplementationType?.Namespace?.StartsWith("StockSim.Web") == true)
                .ToList();
            foreach (var d in hosted) services.Remove(d);
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, NoOpHostedService>());

            // Test auth: a fixed authenticated user with optional Admin role
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                options.DefaultChallengeScheme = TestAuthHandler.Scheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

            services.AddAuthorization();
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var tradingDb = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
            var portfolioDb = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
            await authDb.Database.EnsureDeletedAsync();
            await tradingDb.Database.EnsureDeletedAsync();
            await portfolioDb.Database.EnsureDeletedAsync();
            await authDb.Database.EnsureCreatedAsync();
            await tradingDb.Database.EnsureCreatedAsync();
            await portfolioDb.Database.EnsureCreatedAsync();
        });

        builder.ConfigureLogging(lb =>
        {
            lb.ClearProviders();
            lb.AddConsole();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _tradingConn?.Dispose();
        _portfolioConn?.Dispose();
    }

    private static void RemoveDb<TContext>(IServiceCollection services) where TContext : DbContext
    {
        var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<TContext>) ||
                d.ServiceType == typeof(TContext))
            .ToList();
        foreach (var d in toRemove) services.Remove(d);
    }

    // Helper to create schema once per factory
    public async Task InitializeDatabasesAsync()
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<TradingDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<PortfolioDbContext>().Database.EnsureCreatedAsync();
    }
}

file sealed class NoOpHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

file sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Scheme = "Test";
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // default test identity: trader user
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim(ClaimTypes.Name, "test@stocksim.local"),
            new Claim(ClaimTypes.Email, "test@stocksim.local"),
            new Claim(ClaimTypes.Role, "Trader")
        };
        var identity = new ClaimsIdentity(claims, Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockSim.Application.Abstractions;
using StockSim.Infrastructure.Persistence;

namespace StockSim.IntegrationTests.Util;

public static class ServiceFactory
{
    public static (ServiceProvider sp,
                   IDbContextFactory<ApplicationDbContext> factory,
                   IPortfolioService portfolio)
        Create(string npgsqlConnectionString)
    {
        var services = new ServiceCollection();

        // DbContextFactory for ApplicationDbContext (Postgres)
        services.AddDbContextFactory<ApplicationDbContext>(o => o.UseNpgsql(npgsqlConnectionString));

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        // Use the real PortfolioService implementation
        var portfolio = new StockSim.Web.Services.PortfolioService(factory);

        return (sp, factory, portfolio);
    }
}
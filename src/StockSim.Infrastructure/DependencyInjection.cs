using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StockSim.Application.Abstractions;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Persistence;
using StockSim.Web.Services;

namespace StockSim.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        var cs = cfg.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Missing DefaultConnection.");
        services.AddDatabaseDeveloperPageExceptionFilter();
        
        services.AddDbContextPool<ApplicationDbContext>(o => o.UseNpgsql(cs));
        services.AddPooledDbContextFactory<ApplicationDbContext>(o => o.UseNpgsql(cs));


        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IOrderQueries, OrderQueries>();
        services.AddSingleton<IClock, SystemClock>();

        services.Configure<RabbitOptions>(cfg.GetSection("Rabbit"));
        services.AddSingleton<RabbitConnection>();
        services.AddSingleton<IOrderPublisher, OrderPublisher>();

        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}

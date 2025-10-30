using Microsoft.Extensions.DependencyInjection;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Events;
using StockSim.Application.Integration;
using StockSim.Application.Orders;
using StockSim.Application.Portfolios;

namespace StockSim.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationCore(this IServiceCollection services)
    {
        services.AddSingleton<IEventDispatcher, InMemoryEventDispatcher>();
        services.AddSingleton<IIntegrationEventMapper, DefaultIntegrationEventMapper>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Events;
using StockSim.Application.Orders;

namespace StockSim.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationCore(this IServiceCollection services)
    {
        services.AddSingleton<IEventDispatcher, InMemoryEventDispatcher>();
        services.AddScoped<IOrderService, OrderService>();
        return services;
    }
}

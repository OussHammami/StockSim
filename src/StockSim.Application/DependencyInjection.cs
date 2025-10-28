using Microsoft.Extensions.DependencyInjection;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Events;

namespace StockSim.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationCore(this IServiceCollection services)
    {
        services.AddSingleton<IEventDispatcher, InMemoryEventDispatcher>();
        return services;
    }
}

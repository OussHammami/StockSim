using Microsoft.Extensions.DependencyInjection;
using StockSim.Application.Abstractions.Events;
using StockSim.Domain.Primitives;

namespace StockSim.Application.Events;

/// <summary>Simple sync dispatcher using DI to locate handlers.</summary>
public sealed class InContextEventDispatcher : IEventDispatcher
{
    private readonly IServiceProvider _sp;
    public InContextEventDispatcher(IServiceProvider sp) => _sp = sp;

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var e in events)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(e.GetType());
            var handlers = _sp.GetServices(handlerType);
            foreach (var h in handlers)
            {
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                var task = (Task)method.Invoke(h, new object[] { e, ct })!;
                await task.ConfigureAwait(false);
            }
        }
    }
}

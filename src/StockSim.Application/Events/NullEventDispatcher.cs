using StockSim.Application.Abstractions.Events;
using StockSim.Domain.Primitives;

namespace StockSim.Application.Events;

public sealed class NullEventDispatcher : IEventDispatcher
{
    public Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
        => Task.CompletedTask;
}

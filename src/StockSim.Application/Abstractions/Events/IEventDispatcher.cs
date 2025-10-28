using StockSim.Domain.Primitives;

namespace StockSim.Application.Abstractions.Events;

public interface IEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}

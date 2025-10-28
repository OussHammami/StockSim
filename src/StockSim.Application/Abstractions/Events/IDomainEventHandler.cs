using StockSim.Domain.Primitives;

namespace StockSim.Application.Abstractions.Events;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}

namespace StockSim.Domain.Primitives;

public abstract class Entity
{
    private readonly List<IDomainEvent> _events = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events;

    protected void Raise(IDomainEvent @event) => _events.Add(@event);

    // For unit tests to clear after inspection
    public void ClearDomainEvents() => _events.Clear();
}

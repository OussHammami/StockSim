namespace StockSim.Domain.Primitives;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}

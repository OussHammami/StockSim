using StockSim.Domain.Primitives;

namespace StockSim.Application.Integration;

public interface IIntegrationEventMapper
{
    IEnumerable<IntegrationEvent> Map(IEnumerable<IDomainEvent> domainEvents);
}

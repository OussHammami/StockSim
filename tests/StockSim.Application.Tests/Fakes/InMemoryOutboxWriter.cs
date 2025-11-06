using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;

namespace StockSim.Application.Tests.Fakes;

public sealed class InMemoryOutboxWriter : IOutboxWriter<IPortfolioOutboxContext>
{
    public List<IntegrationEvent> Items { get; } = new();

    public Task WriteAsync(IEnumerable<IntegrationEvent> events, CancellationToken ct = default)
    {
        Items.AddRange(events);
        return Task.CompletedTask;
    }
}

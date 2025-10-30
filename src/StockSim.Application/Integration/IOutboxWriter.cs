namespace StockSim.Application.Integration;

public interface IOutboxWriter
{
    Task WriteAsync(IEnumerable<IntegrationEvent> events, CancellationToken ct = default);
}

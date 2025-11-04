using StockSim.Application.Integration;

namespace StockSim.Application.Abstractions.Outbox
{
    public interface IOutboxWriter<TContextMarker>
    {
        Task WriteAsync(IEnumerable<IntegrationEvent> events, CancellationToken ct = default);
    }
}

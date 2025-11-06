namespace StockSim.Portfolio.Worker.External.Trading;

/// <summary>
/// Handle a single event from Trading. Replace this with an implementation
/// that calls your Application layer to mutate Portfolio.
/// </summary>
public interface ITradingEventHandler
{
    Task HandleAsync(
        string type,          // e.g. "trading.order.filled"
        string dataJson,      // event payload as JSON
        IReadOnlyDictionary<string, string?> headers, // AMQP headers of interest
        CancellationToken ct);
}

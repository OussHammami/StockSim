using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace StockSim.Portfolio.Worker;

/// <summary>
/// Default no-op handler. Logs the event. Safe to keep while wiring your real handler.
/// Swap this by registering your own ITradingEventHandler in DI.
/// </summary>
public sealed class DefaultTradingEventHandler : ITradingEventHandler
{
    private readonly ILogger<DefaultTradingEventHandler> _log;

    public DefaultTradingEventHandler(ILogger<DefaultTradingEventHandler> log) => _log = log;

    public Task HandleAsync(
        string type,
        string dataJson,
        IReadOnlyDictionary<string, string?> headers,
        CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(dataJson);
            _log.LogInformation("Consumed event type={Type} messageId={MessageId} subject={Subject}",
                type,
                headers.TryGetValue("messageId", out var mid) ? mid : null,
                headers.TryGetValue("subject", out var subj) ? subj : null);


            return Task.CompletedTask;
        }
        catch
        {
            // If payload is invalid, let the consumer decide to DLQ or Nack.
            throw;
        }
    }
}

namespace StockSim.Application.Integration;

/// <summary>
/// Idempotency store for consumed integration events.
/// </summary>
public interface IInboxStore
{
    Task<bool> SeenAsync(string dedupeKey, CancellationToken ct = default);   // true => already processed
    Task MarkAsync(string dedupeKey, CancellationToken ct = default);
}

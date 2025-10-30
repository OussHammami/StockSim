using StockSim.Application.Integration;

namespace StockSim.Application.Tests.Fakes;

public sealed class InMemoryInboxStore : IInboxStore
{
    private readonly HashSet<string> _keys = new();

    public Task<bool> SeenAsync(string dedupeKey, CancellationToken ct = default)
        => Task.FromResult(_keys.Contains(dedupeKey));

    public Task MarkAsync(string dedupeKey, CancellationToken ct = default)
    {
        _keys.Add(dedupeKey);
        return Task.CompletedTask;
    }
}

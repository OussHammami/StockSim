namespace StockSim.Application.Abstractions.Inbox
{
    public interface IInboxStore<TContextMarker>
    {
        Task<bool> SeenAsync(string dedupeKey, CancellationToken ct = default); 
        Task MarkAsync(string dedupeKey, CancellationToken ct = default);
    }
}

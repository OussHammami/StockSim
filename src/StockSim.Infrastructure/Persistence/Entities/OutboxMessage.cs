namespace StockSim.Infrastructure.Persistence.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = "";         // e.g., "OrderFilledEvent"
    public string Payload { get; set; } = "";      // JSON
    public DateTimeOffset OccurredUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedUtc { get; set; }
    public int Attempts { get; set; }
}

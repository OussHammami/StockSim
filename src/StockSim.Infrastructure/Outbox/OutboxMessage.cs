namespace StockSim.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = default!;
    public string Source { get; set; } = default!;   // "trading" | "portfolio"
    public string Subject { get; set; } = default!;
    public DateTimeOffset OccurredAt { get; set; }
    public string Data { get; set; } = default!;
    public string SchemaVersion { get; set; } = "1";
    public string? DedupeKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SentAt { get; set; }
    public int Attempts { get; set; }
}

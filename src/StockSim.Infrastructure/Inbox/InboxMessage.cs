namespace StockSim.Infrastructure.Inbox;

public sealed class InboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DedupeKey { get; set; } = default!;
    public DateTimeOffset SeenAt { get; set; } = DateTimeOffset.UtcNow;
}

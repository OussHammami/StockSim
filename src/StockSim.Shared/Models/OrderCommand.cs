namespace StockSim.Shared.Models;

public sealed class OrderCommand
{
    public Guid OrderId { get; init; } = Guid.NewGuid();
    public required string UserId { get; init; }
    public required string Symbol { get; init; }
    public required int Quantity { get; init; }    // +buy, -sell
    public DateTimeOffset SubmittedUtc { get; init; } = DateTimeOffset.UtcNow;
}

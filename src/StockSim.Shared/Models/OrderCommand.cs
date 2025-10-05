namespace StockSim.Shared.Models;

public sealed class OrderCommand
{
    public required string UserId { get; init; }
    public required string Symbol { get; init; }
    public required int Quantity { get; init; }    // +buy, -sell
    public DateTimeOffset SubmittedUtc { get; init; } = DateTimeOffset.UtcNow;
}

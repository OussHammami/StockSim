using StockSim.Domain.Enums;

namespace StockSim.Application.Contracts.Orders;

public sealed class OrderCommand
{
    public Guid OrderId { get; init; } = Guid.NewGuid();
    public required string UserId { get; init; }
    public required string Symbol { get; init; }
    public required int Quantity { get; init; }    // +buy, -sell
    public DateTimeOffset SubmittedUtc { get; init; } = DateTimeOffset.UtcNow;    
    public OrderType Type { get; set; } = OrderType.Market;
    public TimeInForce Tif { get; set; } = TimeInForce.Day;
    public decimal? LimitPrice { get; set; }
    public decimal? StopPrice { get; set; }
}

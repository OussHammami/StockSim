using StockSim.Domain.Enums;

namespace StockSim.Domain.Entities
{
    public class Order
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = default!;
        public string Symbol { get; set; } = "";
        public int Quantity { get; set; }
        public DateTimeOffset SubmittedUtc { get; set; }
        public OrderStatus Status { get; set; }
        public decimal? FillPrice { get; set; }
        public DateTimeOffset? FilledUtc { get; set; }
        public OrderType Type { get; set; } = OrderType.Market;
        public decimal? LimitPrice { get; set; }
        public TimeInForce Tif { get; set; }
        public decimal? StopPrice { get; set; }
        public int Remaining { get; set; }
    }
}

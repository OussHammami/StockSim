using StockSim.Domain.Enums;

namespace StockSim.Infrastructure.Persistence.Entities
{

    public class OrderEntity
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = default!;
        public string Symbol { get; set; } = "";
        public int Quantity { get; set; }
        public DateTimeOffset SubmittedUtc { get; set; }
        public OrderStatus Status { get; set; }
        public decimal? FillPrice { get; set; }
        public DateTimeOffset? FilledUtc { get; set; }
    }
}

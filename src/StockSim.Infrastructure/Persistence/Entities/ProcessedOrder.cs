namespace StockSim.Infrastructure.Persistence.Entities;

public sealed class ProcessedOrder
{
    public Guid OrderId { get; set; }        // PK = idempotency key
}

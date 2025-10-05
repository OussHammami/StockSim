namespace StockSim.Web.Data.Trading;

public sealed class PositionEntity
{
    public string UserId { get; set; } = default!;
    public string Symbol { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal AvgPrice { get; set; }
}

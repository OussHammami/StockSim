namespace StockSim.Domain.Entities;

public sealed class Position
{
    public string Symbol { get; init; } = "";
    public int Quantity { get; set; }
    public decimal AvgPrice { get; set; }
}

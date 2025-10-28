using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Portfolio;

/// <summary>
/// Position is owned by Portfolio aggregate. Average-cost model.
/// </summary>
public sealed class Position
{
    public Symbol Symbol { get; }
    public decimal Quantity { get; private set; }
    public decimal AvgCost { get; private set; } // per unit

    public Position(Symbol symbol)
    {
        Symbol = symbol;
        Quantity = 0m;
        AvgCost = 0m;
    }

    public void ApplyBuy(Quantity qty, Price price)
    {
        var newQty = Quantity + qty.Value;
        var totalCost = (AvgCost * Quantity) + (price.Value * qty.Value);
        AvgCost = newQty == 0 ? 0 : decimal.Round(totalCost / newQty, 4, MidpointRounding.AwayFromZero);
        Quantity = newQty;
    }

    public void ApplySell(Quantity qty)
    {
        if (qty.Value > Quantity) throw new InvalidOperationException("Sell exceeds position.");
        Quantity -= qty.Value;
        if (Quantity == 0m) AvgCost = 0m;
    }
}

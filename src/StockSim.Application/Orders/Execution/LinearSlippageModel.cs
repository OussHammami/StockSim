namespace StockSim.Application.Orders.Execution;

public interface ISlippageModel
{
    decimal AdjustPrice(decimal proposedPrice, decimal quantity, QuoteSnapshot snap);
}

public sealed class LinearSlippageModel : ISlippageModel
{
    private readonly decimal _baseTolerance;      // e.g., 0.0005m = 5 bps
    private readonly decimal _qtyScale;           // e.g., 100 → every 100 units adds one tolerance unit

    public LinearSlippageModel(decimal baseTolerance = 0.0005m, decimal qtyScale = 100m)
    {
        _baseTolerance = baseTolerance;
        _qtyScale = qtyScale;
    }

    public decimal AdjustPrice(decimal proposedPrice, decimal quantity, QuoteSnapshot snap)
    {
        // simplistic: finalPrice = proposedPrice * (1 ± slip)
        var slipFactor = _baseTolerance * (quantity / _qtyScale);
        return proposedPrice * (1 + slipFactor * (quantity >= 0 ? 1 : -1));
    }
}
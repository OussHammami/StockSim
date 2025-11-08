namespace StockSim.Application.Orders.Execution;

public interface IFillPolicy
{
    /// <summary>
    /// Decide how many units to fill this tick, given remaining quantity.
    /// Example: min(remaining, MaxPerFill).
    /// </summary>
    decimal DecideFillQuantity(decimal remaining);
}

public sealed class StaticFillPolicy : IFillPolicy
{
    private readonly decimal _maxPerFill;
    public StaticFillPolicy(decimal maxPerFill) => _maxPerFill = maxPerFill;

    public decimal DecideFillQuantity(decimal remaining) =>
        remaining <= _maxPerFill ? remaining : _maxPerFill;
}
namespace StockSim.Domain.ValueObjects;

public readonly record struct Symbol(string Value)
{
    public override string ToString() => Value;
}

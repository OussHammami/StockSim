using StockSim.Domain.Primitives;

namespace StockSim.Domain.ValueObjects;

/// <summary>Price per unit. Non-negative. Scale = 4.</summary>
public sealed class Price : ValueObject, IComparable<Price>
{
    public decimal Value { get; }

    private Price(decimal value)
    {
        if (value < 0m) throw new ArgumentOutOfRangeException(nameof(value), "Price must be >= 0.");
        Value = decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    public static Price From(decimal value) => new(value);

    public int CompareTo(Price? other) => Value.CompareTo(other?.Value ?? 0m);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString("0.####");
}

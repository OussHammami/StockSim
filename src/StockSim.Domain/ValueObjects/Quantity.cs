using StockSim.Domain.Primitives;

namespace StockSim.Domain.ValueObjects;

/// <summary>Order or position quantity. Positive. Scale = 4 to allow fractional shares if needed.</summary>
public sealed class Quantity : ValueObject, IComparable<Quantity>
{
    public decimal Value { get; }

    private Quantity(decimal value)
    {
        if (value <= 0m) throw new ArgumentOutOfRangeException(nameof(value), "Quantity must be > 0.");
        Value = decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    public static Quantity From(decimal value) => new(value);

    public Quantity Add(Quantity other) => From(Value + other.Value);
    public Quantity Subtract(Quantity other)
    {
        var result = Value - other.Value;
        if (result <= 0m) throw new InvalidOperationException("Resulting quantity must be > 0.");
        return From(result);
    }

    public int CompareTo(Quantity? other) => Value.CompareTo(other?.Value ?? 0m);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString("0.####");
}

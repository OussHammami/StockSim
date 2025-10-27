using StockSim.Domain.Primitives;

namespace StockSim.Domain.ValueObjects;

public sealed class OrderId : ValueObject
{
    public Guid Value { get; }

    private OrderId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("OrderId cannot be empty.", nameof(value));
        Value = value;
    }

    public static OrderId New() => new(Guid.NewGuid());
    public static OrderId From(Guid value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString("N");
}

using StockSim.Domain.Primitives;

namespace StockSim.Domain.ValueObjects;

public class PortfolioId: ValueObject
{
    public Guid Value { get; }

    private PortfolioId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("PortfolioId cannot be empty.", nameof(value));
        Value = value;
    }

    public static PortfolioId New() => new(Guid.NewGuid());
    public static PortfolioId From(Guid value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString("N");
}

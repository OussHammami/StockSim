using StockSim.Domain.Primitives;

namespace StockSim.Domain.ValueObjects;

/// <summary>Currency-less money for the simulation. Scale = 2.</summary>
public sealed class Money : ValueObject, IComparable<Money>
{
    public decimal Amount { get; }
    private Money(decimal amount) => Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    public static Money From(decimal amount) => new(amount);

    public Money Add(Money other) => new(Amount + other.Amount);
    public Money Subtract(Money other) => new(Amount - other.Amount);
    public Money Multiply(decimal factor) => new(Amount * factor);

    public int CompareTo(Money? other) => Amount.CompareTo(other?.Amount ?? 0m);

    public static Money Zero => new(0m);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
    }

    public override string ToString() => Amount.ToString("0.00");
}

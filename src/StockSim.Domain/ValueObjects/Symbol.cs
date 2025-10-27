using System.Text.RegularExpressions;
using StockSim.Domain.Primitives;

namespace StockSim.Domain.ValueObjects;

/// <summary>Ticker symbol. Letters, numbers, dots or dashes. 1–15 chars. Uppercased.</summary>
public sealed class Symbol : ValueObject
{
    public string Value { get; }
    private static readonly Regex Rx = new(@"^[A-Za-z0-9][A-Za-z0-9\.\-]{0,14}$", RegexOptions.Compiled);

    private Symbol(string value) => Value = value.ToUpperInvariant();

    public static Symbol From(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value));
        if (!Rx.IsMatch(value)) throw new ArgumentException("Invalid symbol format.", nameof(value));
        return new Symbol(value.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    // Convenience for dictionary keys
    public static implicit operator string(Symbol s) => s.Value;
}

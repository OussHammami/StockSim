using StockSim.Domain.Primitives;

namespace StockSim.Domain.ValueObjects;

public sealed class Quote : ValueObject
{
    public Symbol Symbol { get; }
    public Price Bid { get; }
    public Price Ask { get; }
    public Price? Last { get; }
    public DateTimeOffset Timestamp { get; }

    private Quote(Symbol symbol, Price bid, Price ask, Price? last, DateTimeOffset ts)
    {
        if (ask.Value < bid.Value) throw new ArgumentException("Ask < Bid.");
        Symbol = symbol;
        Bid = bid;
        Ask = ask;
        Last = last;
        Timestamp = ts;
    }

    public static Quote From(Symbol symbol, Price bid, Price ask, Price? last, DateTimeOffset ts)
        => new(symbol, bid, ask, last, ts);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Symbol;
        yield return Bid;
        yield return Ask;
        yield return Last;
        yield return Timestamp;
    }
}

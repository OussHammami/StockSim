using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Tests.ValueObjects;
public class QuantityPriceSymbolTests
{
    [Fact]
    public void Quantity_Must_Be_Positive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Quantity.From(0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Quantity.From(-1m));
    }

    [Fact]
    public void Price_Must_Be_NonNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Price.From(-0.01m));
        Assert.Equal(1.2346m, Price.From(1.23456m).Value);
    }

    [Fact]
    public void Symbol_Validates_Format()
    {
        _ = Symbol.From("AAPL");
        _ = Symbol.From("BRK.B");
        _ = Symbol.From("STM-IT");
        Assert.Throws<ArgumentNullException>(() => Symbol.From(""));
        Assert.Throws<ArgumentException>(() => Symbol.From(" TOO_LONG_SYMBOL_123 "));
        Assert.Equal("AAPL", Symbol.From("aapl").Value);
    }
}

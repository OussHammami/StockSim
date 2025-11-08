using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Tests.ValueObjects;

public class SymbolTests
{
    [Theory]
    [InlineData("AAPL")]
    [InlineData("msft")]
    public void From_Normalizes_And_Preserves_Value(string input)
    {
        var s = Symbol.From(input);
        s.Value.Should().Be(input.Trim().ToUpperInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void From_Empty_Throws(string input)
    {
        Action act = () => Symbol.From(input);
        act.Should().Throw<Exception>();
    }
}
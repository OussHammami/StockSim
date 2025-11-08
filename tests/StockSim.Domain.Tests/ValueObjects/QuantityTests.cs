using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Tests.ValueObjects;

public class QuantityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void From_NonPositive_Throws(decimal v)
    {
        Action act = () => Quantity.From(v);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void From_Positive_Succeeds()
    {
        var q = Quantity.From(1.2345m);
        q.Value.Should().Be(1.2345m);
    }
}
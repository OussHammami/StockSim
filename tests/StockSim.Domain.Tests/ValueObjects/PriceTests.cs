using FluentAssertions;
using StockSim.Domain.ValueObjects;
using Xunit;

namespace StockSim.Domain.Tests.ValueObjects;

public class PriceTests
{
    [Theory]
    [InlineData(-0.01)]
    public void From_NonPositive_Throws(decimal v)
    {
        Action act = () => Price.From(v);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void From_Positive_Succeeds()
    {
        var p = Price.From(123.45m);
        p.Value.Should().Be(123.45m);
    }
}
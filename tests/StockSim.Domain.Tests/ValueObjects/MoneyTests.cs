using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Tests.ValueObjects;
public class MoneyTests
{
    [Fact]
    public void Rounds_To_Two_Decimals()
    {
        var m = Money.From(1.234m);
        Assert.Equal(1.23m, m.Amount);
    }

    [Fact]
    public void Equality_On_Amount()
    {
        Assert.Equal(Money.From(10.00m), Money.From(10m));
        Assert.NotEqual(Money.From(10m), Money.From(9.99m));
    }

    [Fact]
    public void Arithmetic_Works()
    {
        var a = Money.From(5m);
        var b = Money.From(2m);
        Assert.Equal(Money.From(7m), a.Add(b));
        Assert.Equal(Money.From(3m), a.Subtract(b));
    }
}
    
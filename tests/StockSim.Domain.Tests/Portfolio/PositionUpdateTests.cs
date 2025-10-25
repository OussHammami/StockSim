using StockSim.Domain.Entities;
using Xunit;

namespace StockSim.Domain.Tests.Portfolio;

public sealed class PositionUpdateTests
{
    private static void ApplyBuy(Position p, int qty, decimal price)
    {
        // VWAP averaging
        var newQty = p.Quantity + qty;
        var newCost = p.AvgPrice * p.Quantity + price * qty;
        p.Quantity = newQty;
        p.AvgPrice = newQty == 0 ? 0 : newCost / newQty;
    }

    private static void ApplySell(Position p, int qty, decimal price)
    {
        // Reduce quantity; avg price unchanged on sell
        p.Quantity -= qty;
        if (p.Quantity == 0) p.AvgPrice = 0; // flat position resets avg price
    }

    [Fact]
    public void Given_EmptyPosition_When_Buy_Then_QuantityAndAvgPriceSet()
    {
        var p = new Position { Symbol = "AAPL", Quantity = 0, AvgPrice = 0 };
        ApplyBuy(p, 10, 100m);

        Assert.Equal(10, p.Quantity);
        Assert.Equal(100m, p.AvgPrice);
    }

    [Fact]
    public void Given_Position_When_AverageUpAndDown_Then_AvgPriceVWAPCorrect()
    {
        var p = new Position { Symbol = "AAPL", Quantity = 10, AvgPrice = 100m };
        ApplyBuy(p, 10, 110m); // 10@100 + 10@110 => 20 @ 105

        Assert.Equal(20, p.Quantity);
        Assert.Equal(105m, p.AvgPrice);
    }

    [Fact]
    public void Given_Position_When_Sell_Partial_Then_AvgPriceUnchanged()
    {
        var p = new Position { Symbol = "AAPL", Quantity = 20, AvgPrice = 105m };
        ApplySell(p, 5, 120m);

        Assert.Equal(15, p.Quantity);
        Assert.Equal(105m, p.AvgPrice);
    }

    [Fact]
    public void Given_Position_When_Sell_ToFlat_Then_QuantityZeroAndAvgPriceZero()
    {
        var p = new Position { Symbol = "AAPL", Quantity = 15, AvgPrice = 105m };
        ApplySell(p, 15, 120m);

        Assert.Equal(0, p.Quantity);
        Assert.Equal(0m, p.AvgPrice);
    }
}

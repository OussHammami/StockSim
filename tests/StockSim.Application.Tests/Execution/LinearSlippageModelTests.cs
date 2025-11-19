using FluentAssertions;
using StockSim.Application.Orders.Execution;

public class LinearSlippageModelTests
{
    [Fact]
    public void AdjustPrice_Scales_With_Position_Size()
    {
        var model = new LinearSlippageModel(baseTolerance: 0.0005m, qtyScale: 100m);
        var snap = new QuoteSnapshot("AAPL", 99.5m, 100m, 99.75m, DateTimeOffset.UtcNow);

        var adjusted = model.AdjustPrice(100m, 200m, snap);

        adjusted.Should().Be(100.1m); // 100 * (1 + 0.001)
    }

    [Fact]
    public void AdjustPrice_Applies_Sign_For_Short_Side()
    {
        var model = new LinearSlippageModel(baseTolerance: 0.0005m, qtyScale: 100m);
        var snap = new QuoteSnapshot("AAPL", 99.5m, 100m, 99.75m, DateTimeOffset.UtcNow);

        var adjusted = model.AdjustPrice(100m, -150m, snap);

        adjusted.Should().Be(100m * (1 + 0.0005m * 1.5m));
        adjusted.Should().Be(100.075m);
    }
}

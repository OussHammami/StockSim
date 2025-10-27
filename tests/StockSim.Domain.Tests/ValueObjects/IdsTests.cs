using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Tests.ValueObjects;
public class IdsTests
{
    [Fact]
    public void OrderId_Not_Empty()
    {
        Assert.Throws<ArgumentException>(() => OrderId.From(Guid.Empty));
        var id = OrderId.New();
        Assert.False(id.Value == Guid.Empty);
    }

    [Fact]
    public void PortfolioId_Not_Empty()
    {
        Assert.Throws<ArgumentException>(() => PortfolioId.From(Guid.Empty));
        var id = PortfolioId.New();
        Assert.False(id.Value == Guid.Empty);
    }
}

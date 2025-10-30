using StockSim.Application.Integration;
using StockSim.Domain.Orders.Events;
using StockSim.Domain.ValueObjects;

public class DedupeKeyTests
{
    [Fact]
    public void PartiallyFilled_Key_Varies_With_CumFilled()
    {
        var mapper = new DefaultIntegrationEventMapper();
        var id = OrderId.New();

        var e1 = new OrderPartiallyFilled(id, 1m, 100m, 1m);
        var e2 = new OrderPartiallyFilled(id, 1m, 101m, 2m);

        var i1 = mapper.Map(new[] { e1 }).Single();
        var i2 = mapper.Map(new[] { e2 }).Single();

        Assert.NotEqual(i1.DedupeKey, i2.DedupeKey);
    }
}

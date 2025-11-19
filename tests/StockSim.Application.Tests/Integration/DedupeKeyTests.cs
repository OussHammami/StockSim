using System.Linq;
using StockSim.Application.Integration;
using StockSim.Domain.Orders;
using StockSim.Domain.Orders.Events;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Tests.Integration;

public class DedupeKeyTests
{
    private static readonly Guid U = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff");

    [Fact]
    public void PartiallyFilled_Key_Varies_With_CumFilled()
    {
        var mapper = new DefaultIntegrationEventMapper();
        var id = OrderId.New();

        var e1 = new OrderFillApplied(U, id, Symbol.From("APPLT"), OrderSide.Buy, 1m, 100m, 1m);
        var e2 = new OrderFillApplied(U, id, Symbol.From("APPLT"), OrderSide.Buy, 1m, 101m, 2m);

        var events1 = mapper.Map(new[] { e1 }).ToDictionary(e => e.Type);
        var events2 = mapper.Map(new[] { e2 }).ToDictionary(e => e.Type);

        Assert.Equal(events1.Keys, events2.Keys);

        foreach (var type in events1.Keys)
        {
            Assert.NotEqual(events1[type].DedupeKey, events2[type].DedupeKey);
        }
    }
}

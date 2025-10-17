using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockSim.Domain.Enums;
using StockSim.Infrastructure.Persistence;
using StockSim.Infrastructure.Persistence.Entities;
using StockSim.IntegrationTests.Containers;
using StockSim.IntegrationTests.Util;
using Xunit;

namespace StockSim.IntegrationTests.Orders;

[Collection(nameof(ContainersCollection))]
public sealed class OrderCrudTests(ContainersFixture fx)
{
    private readonly ContainersFixture _fx = fx;

    [Fact]
    public async Task Migrations_apply_and_order_persists()
    {
        await using var db = await MigrationsHelper.ApplyAsync(_fx.PgConnectionString);

        var id = Guid.NewGuid();
        db.Orders.Add(new OrderEntity
        {
            OrderId = id,
            UserId = "it-user",
            Symbol = "MSFT",
            Quantity = 10,
            Remaining = 10,
            Status = OrderStatus.Pending,
            Type = OrderType.Market,
            Tif = TimeInForce.Day,
            SubmittedUtc = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync();

        var reloaded = await db.Orders.SingleAsync(o => o.OrderId == id);
        Assert.Equal("MSFT", reloaded.Symbol);
        Assert.Equal(10, reloaded.Remaining);
        Assert.Equal(OrderStatus.Pending, reloaded.Status);
    }
}
using Microsoft.EntityFrameworkCore;
using StockSim.Application.Abstractions.Paging;
using StockSim.Application.Contracts.Orders;
using StockSim.Domain.Enums;
using StockSim.Infrastructure.Persistence;
using StockSim.Infrastructure.Persistence.Entities;
using StockSim.Web.Services;
using StockSim.IntegrationTests.Containers;
using StockSim.IntegrationTests.Util;
using Xunit;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace StockSim.IntegrationTests.Orders;

[Collection(nameof(ContainersCollection))]
public sealed class OrderQueriesTests(ContainersFixture fx)
{
    private readonly ContainersFixture _fx = fx;

    [Fact]
    public async Task GetPage_returns_orders_with_expected_fields()
    {
        var userId = $"it-user-{Guid.NewGuid()}";
        await using var _ = await MigrationsHelper.ApplyAsync(_fx.PgConnectionString);

        // Seed two orders
        await using (var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_fx.PgConnectionString).Options))
        {
            db.Orders.AddRange(
                new OrderEntity
                {
                    OrderId = Guid.NewGuid(),
                    UserId = userId, Symbol = "MSFT", Quantity = 5, Remaining = 5,
                    Type = OrderType.Market, Tif = TimeInForce.Day, Status = OrderStatus.Pending,
                    SubmittedUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
                },
                new OrderEntity
                {
                    OrderId = Guid.NewGuid(),
                    UserId = userId, Symbol = "AAPL", Quantity = -3, Remaining = 0,
                    Type = OrderType.Limit, Tif = TimeInForce.Day, LimitPrice = 150m,
                    Status = OrderStatus.Filled, SubmittedUtc = DateTimeOffset.UtcNow
                });
            await db.SaveChangesAsync();
        }

        // Query
        var factory = new PooledDbContextFactory<ApplicationDbContext>(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_fx.PgConnectionString).Options);
        var queries = new OrderQueries(factory);

        PageResult<StockSim.Domain.Entities.Order> page = await queries.GetPageAsync(userId, skip: 0, take: 10);

        Assert.True(page.Total >= 2);
        Assert.Contains(page.Items, o => o.Symbol == "MSFT" && o.Quantity == 5 && o.Status == OrderStatus.Pending);
        Assert.Contains(page.Items, o => o.Symbol == "AAPL" && o.Quantity == -3 && o.Type == OrderType.Limit && o.LimitPrice == 150m);
    }
}
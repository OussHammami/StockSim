using System.Linq;
using System.Reflection;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;
using StockSim.Infrastructure.Persistence.Trading;
using StockSim.Infrastructure.Repositories;
using Xunit;

namespace StockSim.IntegrationTests.Orders;

public class OrderRepositoryTests : IAsyncLifetime
{
    private readonly IContainer _pg;
    private string _connStr = default!;

    public OrderRepositoryTests()
    {
        _pg = new ContainerBuilder()
            .WithImage("postgres:16-alpine")
            .WithEnvironment("POSTGRES_USER", "u")
            .WithEnvironment("POSTGRES_PASSWORD", "p")
            .WithEnvironment("POSTGRES_DB", "db")
            .WithPortBinding(0, 5432)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();
        var hostPort = _pg.GetMappedPublicPort(5432);
        _connStr = $"Host=localhost;Port={hostPort};Database=db;Username=u;Password=p;Include Error Detail=true";

        var opts = new DbContextOptionsBuilder<TradingDbContext>()
            .UseNpgsql(_connStr, b => b.MigrationsAssembly(typeof(TradingDbContext).Assembly.FullName))
            .Options;

        using var db = new TradingDbContext(opts);
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _pg.DisposeAsync().AsTask();

    [Fact]
    public async Task Add_And_GetByUser_Works()
    {
        await WithRepository(async repo =>
        {
            var order = CreateLimit("AAPL", OrderSide.Buy, 10m, 123.45m, DateTime.UtcNow);
            await repo.AddAsync(order);
            await repo.SaveChangesAsync();

            var fetched = await repo.GetByUserAsync(order.UserId, take: 10);
            fetched.Should().ContainSingle(x => x.Id == order.Id);
        });
    }

    [Fact]
    public async Task GetOpenBySymbolAsync_Filters_And_Sorts()
    {
        await WithRepository(async repo =>
        {
            var symbol = "SHOP";
            var early = CreateLimit(symbol, OrderSide.Buy, 5m, 100m, DateTime.UtcNow.AddMinutes(-10));
            var later = CreateLimit(symbol, OrderSide.Buy, 5m, 101m, DateTime.UtcNow.AddMinutes(-5));
            later.ApplyFill(Quantity.From(1m), Price.From(101m));

            var canceled = CreateLimit(symbol, OrderSide.Buy, 5m, 102m, DateTime.UtcNow.AddMinutes(-3));
            canceled.Cancel();

            var other = CreateLimit("MSFT", OrderSide.Buy, 5m, 105m, DateTime.UtcNow.AddMinutes(-1));

            await repo.AddAsync(early);
            await repo.AddAsync(later);
            await repo.AddAsync(canceled);
            await repo.AddAsync(other);
            await repo.SaveChangesAsync();

            var open = await repo.GetOpenBySymbolAsync(Symbol.From(symbol));

            open.Select(o => o.Id).Should().Equal(early.Id, later.Id);
            open.Should().OnlyContain(o => o.State == OrderState.Accepted || o.State == OrderState.PartiallyFilled);
        });
    }

    [Fact]
    public async Task PriceFilteredSideQueries_Respect_Limits()
    {
        await WithRepository(async repo =>
        {
            var symbol = "NFLX";
            var buyMarket = Order.CreateMarket(Guid.NewGuid(), Symbol.From(symbol), OrderSide.Buy, Quantity.From(3m));
            buyMarket.Accept();
            buyMarket.ClearDomainEvents();

            var buyHigh = CreateLimit(symbol, OrderSide.Buy, 4m, 120m, DateTime.UtcNow.AddMinutes(-4));
            var buyLow = CreateLimit(symbol, OrderSide.Buy, 4m, 90m, DateTime.UtcNow.AddMinutes(-3));
            var sellLow = CreateLimit(symbol, OrderSide.Sell, 4m, 95m, DateTime.UtcNow.AddMinutes(-2));
            var sellHigh = CreateLimit(symbol, OrderSide.Sell, 4m, 130m, DateTime.UtcNow.AddMinutes(-1));
            var sellMarket = Order.CreateMarket(Guid.NewGuid(), Symbol.From(symbol), OrderSide.Sell, Quantity.From(2m));
            sellMarket.Accept();
            sellMarket.ClearDomainEvents();

            var other = CreateLimit("AAPL", OrderSide.Buy, 5m, 150m, DateTime.UtcNow);

            await repo.AddAsync(buyMarket);
            await repo.AddAsync(buyHigh);
            await repo.AddAsync(buyLow);
            await repo.AddAsync(sellLow);
            await repo.AddAsync(sellHigh);
            await repo.AddAsync(sellMarket);
            await repo.AddAsync(other);
            await repo.SaveChangesAsync();

            var buys = await repo.GetOpenBuysAtOrAboveAsync(Symbol.From(symbol), 100m);
            buys.Should().OnlyContain(o => o.Side == OrderSide.Buy);
            buys.Select(o => o.Id).Should().BeEquivalentTo(new[] { buyMarket.Id, buyHigh.Id });

            var sells = await repo.GetOpenSellsAtOrBelowAsync(Symbol.From(symbol), 100m);
            sells.Should().OnlyContain(o => o.Side == OrderSide.Sell);
            sells.Select(o => o.Id).Should().BeEquivalentTo(new[] { sellLow.Id, sellMarket.Id });
        });
    }

    private DbContextOptions<TradingDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<TradingDbContext>().UseNpgsql(_connStr).Options;

    private async Task WithRepository(Func<OrderRepository, Task> work)
    {
        await using var db = new TradingDbContext(CreateOptions());
        await db.Orders.ExecuteDeleteAsync();
        var repo = new OrderRepository(db);
        await work(repo);
    }

    private static Order CreateLimit(string symbol, OrderSide side, decimal qty, decimal price, DateTime createdAt)
    {
        var order = Order.CreateLimit(Guid.NewGuid(), Symbol.From(symbol), side, Quantity.From(qty), Price.From(price));
        order.Accept();
        order.ClearDomainEvents();
        typeof(Order).GetProperty(nameof(Order.CreatedAt), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(order, createdAt);
        return order;
    }
}

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
        var opts = new DbContextOptionsBuilder<TradingDbContext>()
            .UseNpgsql(_connStr)
            .Options;

        await using var db = new TradingDbContext(opts);
        var repo = new OrderRepository(db);

        var order = Order.CreateLimit(Guid.NewGuid(), Symbol.From("AAPL"), OrderSide.Buy, Quantity.From(10), Price.From(123.45m));
        order.Accept();
        await repo.AddAsync(order);
        await repo.SaveChangesAsync();

        var fetched = await repo.GetByUserAsync(order.UserId, take: 10);
        fetched.Should().ContainSingle(x => x.Id.ToString() == order.Id.ToString());
    }
}
using Microsoft.EntityFrameworkCore;
using StockSim.Infrastructure.Persistence;
using StockSim.Infrastructure.Persistence.Entities;
using StockSim.IntegrationTests.Containers;
using StockSim.IntegrationTests.Util;
using Xunit;

namespace StockSim.IntegrationTests.Portfolio;

[Collection(nameof(ContainersCollection))]
public sealed class PortfolioServiceTests(ContainersFixture fx)
{
    private readonly ContainersFixture _fx = fx;

    [Fact]
    public async Task Buy_increases_position_and_reduces_cash()
    {
        var userId = $"it-user-{Guid.NewGuid()}";
        await using var _ = await MigrationsHelper.ApplyAsync(_fx.PgConnectionString);
        var (sp, factory, portfolio) = ServiceFactory.Create(_fx.PgConnectionString);

        // Buy 10 @ 100
        var ok = await portfolio.TryTradeAsync(userId, "MSFT", qty: 10, price: 100m, CancellationToken.None);
        Assert.True(ok);

        await using var db = await factory.CreateDbContextAsync();
        var pf = await db.Portfolios.AsNoTracking().SingleAsync(p => p.UserId == userId);
        var pos = await db.Positions.AsNoTracking().SingleAsync(p => p.UserId == userId && p.Symbol == "MSFT");

        Assert.Equal(100_000m - (10 * 100m), pf.Cash);
        Assert.Equal(10, pos.Quantity);
        Assert.Equal(100m, pos.AvgPrice);
        await sp.DisposeAsync();
    }

    [Fact]
    public async Task Sell_reduces_or_removes_position_and_increases_cash()
    {
        var userId = $"it-user-{Guid.NewGuid()}";
        await using var _ = await MigrationsHelper.ApplyAsync(_fx.PgConnectionString);
        var (sp, factory, portfolio) = ServiceFactory.Create(_fx.PgConnectionString);

        // Seed: buy 10 @ 100
        Assert.True(await portfolio.TryTradeAsync(userId, "AAPL", qty: 10, price: 100m, CancellationToken.None));

        // Sell 10 @ 110
        Assert.True(await portfolio.TryTradeAsync(userId, "AAPL", qty: -10, price: 110m, CancellationToken.None));

        await using var db = await factory.CreateDbContextAsync();
        var pf = await db.Portfolios.AsNoTracking().SingleAsync(p => p.UserId == userId);
        var posExists = await db.Positions.AnyAsync(p => p.UserId == userId && p.Symbol == "AAPL");

        // Cash back to 100_000 + realized profit 10*(110-100) == 100_100
        Assert.Equal(100_100m, pf.Cash);
        Assert.False(posExists);
        await sp.DisposeAsync();
    }

    [Fact]
    public async Task Insufficient_cash_rejects_buy()
    {
        var userId = $"it-user-{Guid.NewGuid()}";
        await using var _ = await MigrationsHelper.ApplyAsync(_fx.PgConnectionString);
        var (sp, factory, portfolio) = ServiceFactory.Create(_fx.PgConnectionString);

        string? err = null;
        // Cost = 200,000 > 100,000
        var ok = await portfolio.TryTradeAsync(userId, "NVDA", qty: 2_000, price: 100m, CancellationToken.None, r => err = r);

        Assert.False(ok);
        Assert.Equal("Insufficient cash.", err);

        await using var db = await factory.CreateDbContextAsync();
        var pf = await db.Portfolios.AsNoTracking().SingleAsync(p => p.UserId == userId);
        Assert.Equal(100_000m, pf.Cash);
        Assert.False(await db.Positions.AnyAsync(p => p.UserId == userId));
        await sp.DisposeAsync();
    }

    [Fact]
    public async Task Insufficient_shares_rejects_sell()
    {
        var userId = $"it-user-{Guid.NewGuid()}";
        await using var _ = await MigrationsHelper.ApplyAsync(_fx.PgConnectionString);
        var (sp, factory, portfolio) = ServiceFactory.Create(_fx.PgConnectionString);

        // No shares yet
        string? err = null;
        var ok = await portfolio.TryTradeAsync(userId, "AMZN", qty: -5, price: 90m, CancellationToken.None, r => err = r);

        Assert.False(ok);
        Assert.Equal("Insufficient shares.", err);

        await using var db = await factory.CreateDbContextAsync();
        var pf = await db.Portfolios.AsNoTracking().SingleAsync(p => p.UserId == userId);
        Assert.Equal(100_000m, pf.Cash);
        Assert.False(await db.Positions.AnyAsync(p => p.UserId == userId));
        await sp.DisposeAsync();
    }
}
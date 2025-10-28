using StockSim.Domain.Orders;
using StockSim.Domain.Portfolio.Events;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Tests.Portfolio;
public class PortfolioTests
{
    private static readonly Guid U = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff");

    [Fact]
    public void Deposit_And_Reserve_Funds()
    {
        var p = new Domain.Portfolio.Portfolio(PortfolioId.New(), U);
        p.Deposit(Money.From(1000m));

        var order = OrderId.New();
        p.ReserveFunds(order, Money.From(300m));

        Assert.Equal(700m, p.AvailableCash().Amount);
        Assert.Contains(p.DomainEvents, e => e is FundsReserved);
    }

    [Fact]
    public void Reserve_Funds_Fails_When_Insufficient()
    {
        var p = new Domain.Portfolio.Portfolio(PortfolioId.New(), U);
        p.Deposit(Money.From(100m));
        Assert.Throws<InvalidOperationException>(() => p.ReserveFunds(OrderId.New(), Money.From(150m)));
    }

    [Fact]
    public void Reserve_And_Release_Shares()
    {
        var p = new Domain.Portfolio.Portfolio(PortfolioId.New(), U);
        var sym = Symbol.From("AAPL");
        // seed position
        p.ApplyFill(OrderId.New(), OrderSide.Buy, sym, Quantity.From(10), Price.From(10));
        p.ClearDomainEvents();

        var oid = OrderId.New();
        p.ReserveShares(oid, sym, Quantity.From(6));
        Assert.Equal(6m, p.ReservedFor(sym));

        p.ReleaseShares(oid, sym, Quantity.From(2), "adjust");
        Assert.Equal(4m, p.ReservedFor(sym));
    }

    [Fact]
    public void Buy_Fill_Consumes_Cash_And_Increases_Position()
    {
        var p = new Domain.Portfolio.Portfolio(PortfolioId.New(), U);
        var sym = Symbol.From("MSFT");
        p.Deposit(Money.From(1000m));
        var oid = OrderId.New();

        p.ReserveFunds(oid, Money.From(500m));
        p.ClearDomainEvents();

        p.ApplyFill(oid, OrderSide.Buy, sym, Quantity.From(5), Price.From(100));

        Assert.Equal(500m, p.Cash.Amount);
        var pos = p.Positions["MSFT"];
        Assert.Equal(5m, pos.Quantity);
        Assert.True(p.DomainEvents.OfType<FillApplied>().Any());
    }

    [Fact]
    public void Sell_Fill_Increases_Cash_And_Reduces_Position()
    {
        var p = new Domain.Portfolio.Portfolio(PortfolioId.New(), U);
        var sym = Symbol.From("TSLA");
        p.Deposit(Money.From(1000m));
        // own 10 at avg 100
        p.ApplyFill(OrderId.New(), OrderSide.Buy, sym, Quantity.From(10), Price.From(100));
        p.ClearDomainEvents();

        var oid = OrderId.New();
        p.ReserveShares(oid, sym, Quantity.From(4));
        p.ClearDomainEvents();

        p.ApplyFill(oid, OrderSide.Sell, sym, Quantity.From(4), Price.From(120));

        var pos = p.Positions["TSLA"];
        Assert.Equal(6m, pos.Quantity);
        Assert.Equal(480m, p.Cash.Amount); // 4 * 120
        Assert.Equal(0m, p.ReservedFor(sym));
        Assert.True(p.DomainEvents.OfType<FillApplied>().Any());
    }
}

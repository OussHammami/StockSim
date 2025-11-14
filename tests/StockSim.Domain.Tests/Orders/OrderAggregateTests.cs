using StockSim.Domain.Orders;
using StockSim.Domain.Orders.Events;
using StockSim.Domain.ValueObjects;

namespace StockSim.Domain.Tests.Orders;

public class OrderAggregateTests
{
    private static readonly Guid U = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff");

    [Fact]
    public void Create_Accept_Limit_Order()
    {
        var o = Order.CreateLimit(U, Symbol.From("AAPL"), OrderSide.Buy, Quantity.From(10), Price.From(100));
        Assert.Equal(OrderState.New, o.State);

        o.Accept();
        Assert.Equal(OrderState.Accepted, o.State);
        Assert.Single(o.DomainEvents);
        Assert.IsType<OrderAccepted>(o.DomainEvents.First());
    }

    [Fact]
    public void Fill_Partial_Then_Complete()
    {
        var o = Order.CreateLimit(U, Symbol.From("AAPL"), OrderSide.Buy, Quantity.From(10), Price.From(100));
        o.Accept();

        o.ApplyFill(Quantity.From(4), Price.From(100));
        Assert.Equal(6m, o.RemainingQuantity);
        Assert.Equal(OrderState.PartiallyFilled, o.State);
        Assert.True(o.DomainEvents.OfType<OrderFillApplied>().Any());

        o.ClearDomainEvents();

        o.ApplyFill(Quantity.From(6), Price.From(101));
        Assert.Equal(0m, o.RemainingQuantity);
        Assert.Equal(OrderState.Filled, o.State);
        var filled = Assert.IsType<OrderFillComplete>(o.DomainEvents.Last());
        Assert.Equal(10m, filled.TotalFilledQuantity);
        Assert.True(filled.AverageFillPrice >= 100m && filled.AverageFillPrice <= 101m);
    }

    [Fact]
    public void Overfill_Throws()
    {
        var o = Order.CreateMarket(U, Symbol.From("MSFT"), OrderSide.Sell, Quantity.From(5));
        o.Accept();

        Assert.Throws<InvalidOperationException>(() => o.ApplyFill(Quantity.From(6), Price.From(300)));
    }

    [Fact]
    public void Reject_From_New_Emits_Event()
    {
        var o = Order.CreateMarket(U, Symbol.From("TSLA"), OrderSide.Sell, Quantity.From(1));
        o.Reject("invalid qty");
        Assert.Equal(OrderState.Rejected, o.State);
        Assert.IsType<OrderRejected>(o.DomainEvents.Single());
    }

    [Fact]
    public void Cancel_From_Accepted_Emits_Event()
    {
        var o = Order.CreateMarket(U, Symbol.From("NFLX"), OrderSide.Buy, Quantity.From(2));
        o.Accept();
        o.ClearDomainEvents();
        o.Cancel("user requested");
        Assert.Equal(OrderState.Canceled, o.State);
        Assert.IsType<OrderCanceled>(o.DomainEvents.Single());
    }
    [Fact]
    public void Accept_Twice_Throws()
    {
        var o = Order.CreateMarket(U, Symbol.From("AAPL"), OrderSide.Buy, Quantity.From(1));
        o.Accept();
        Action act = () => o.Accept();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Fill_FromNew_Throws()
    {
        var o = Order.CreateMarket(U, Symbol.From("AAPL"), OrderSide.Buy, Quantity.From(1));
        Action act = () => o.ApplyFill(Quantity.From(1), Price.From(10));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Fill_Exceeding_Remaining_Throws()
    {
        var o = Order.CreateMarket(U, Symbol.From("AAPL"), OrderSide.Buy, Quantity.From(2));
        o.Accept();
        o.ApplyFill(Quantity.From(1), Price.From(10));
        Action act = () => o.ApplyFill(Quantity.From(2), Price.From(11));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Market_With_LimitPrice_Throws()
    {
        Action act = () => Order.CreateMarket(U, Symbol.From("AAPL"), OrderSide.Sell, Quantity.From(1) /* market */);
        act.Should().NotThrow();
    }

    [Fact]
    public void Limit_With_Null_Price_Throws()
    {
        Action act = () => Order.CreateLimit(U, Symbol.From("AAPL"), OrderSide.Sell, Quantity.From(1), null!);
        act.Should().Throw<ArgumentException>();
    }
}

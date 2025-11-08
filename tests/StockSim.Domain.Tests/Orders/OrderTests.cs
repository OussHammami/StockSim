using FluentAssertions;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;
using Xunit;

namespace StockSim.Domain.Tests.Orders;

public class OrderTests
{
    private static readonly Guid User = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public void CreateMarket_SetsFields_AndStateNew()
    {
        var o = Order.CreateMarket(User, Symbol.From("AAPL"), OrderSide.Buy, Quantity.From(10));
        o.UserId.Should().Be(User);
        o.Symbol.Value.Should().Be("AAPL");
        o.Side.Should().Be(OrderSide.Buy);
        o.Type.Should().Be(OrderType.Market);
        o.Quantity.Value.Should().Be(10);
        o.State.Should().Be(OrderState.New);
        o.LimitPrice.Should().BeNull();
    }

    [Fact]
    public void CreateLimit_Requires_LimitPrice()
    {
        Action act = () => Order.CreateLimit(User, Symbol.From("AAPL"), OrderSide.Buy, Quantity.From(1), Price.From(0));
        act.Should().NotThrow();
    }

    [Fact]
    public void CreateLimit_Throws_When_NoLimitPrice()
    {
        Action act = () => new Action(() => Order.CreateLimit(User, Symbol.From("AAPL"), OrderSide.Buy, Quantity.From(1), null!)).Invoke();
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Accept_FromNew_TransitionsToAccepted_AndRaisesEvent()
    {
        var o = Order.CreateMarket(User, Symbol.From("AAPL"), OrderSide.Buy, Quantity.From(5));
        o.Accept();
        o.State.Should().Be(OrderState.Accepted);
        o.DomainEvents.Should().NotBeEmpty();
    }

    [Fact]
    public void ApplyFill_Partial_UpdatesAverages_AndStatePartiallyFilled()
    {
        var o = Order.CreateMarket(User, Symbol.From("AAPL"), OrderSide.Buy, Quantity.From(10));
        o.Accept();
        o.ApplyFill(Quantity.From(3), Price.From(100));

        o.FilledQuantity.Should().Be(3);
        o.RemainingQuantity.Should().Be(7);
        o.AverageFillPrice.Should().Be(100m);
        o.State.Should().Be(OrderState.PartiallyFilled);
    }

    [Fact]
    public void ApplyFill_FinalFill_TransitionsToFilled()
    {
        var o = Order.CreateMarket(User, Symbol.From("AAPL"), OrderSide.Sell, Quantity.From(10));
        o.Accept();
        o.ApplyFill(Quantity.From(4), Price.From(10));
        o.ApplyFill(Quantity.From(6), Price.From(12));

        o.State.Should().Be(OrderState.Filled);
        o.FilledQuantity.Should().Be(10);
        o.RemainingQuantity.Should().Be(0);
        o.AverageFillPrice.Should().BeApproximately(11.2m, 0.0001m);
    }

    [Fact]
    public void Reject_FromInvalidStates_Throws()
    {
        var o = Order.CreateMarket(User, Symbol.From("AAPL"), OrderSide.Buy, Quantity.From(1));
        o.Accept();
        o.ApplyFill(Quantity.From(1), Price.From(10));

        Action act = () => o.Reject("too late");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_FromFilled_Throws()
    {
        var o = Order.CreateMarket(User, Symbol.From("AAPL"), OrderSide.Buy, Quantity.From(1));
        o.Accept();
        o.ApplyFill(Quantity.From(1), Price.From(10));
        Action act = () => o.Cancel("no need");
        act.Should().Throw<InvalidOperationException>();
    }
}
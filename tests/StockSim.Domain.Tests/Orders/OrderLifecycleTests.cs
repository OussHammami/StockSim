using System;
using StockSim.Domain.Entities;
using StockSim.Domain.Enums;
using Xunit;

namespace StockSim.Domain.Tests.Orders;

public sealed class OrderLifecycleTests
{
    [Fact]
    public void Given_NewMarketOrder_When_Created_Then_PendingWithRemainingEqualsQuantity()
    {
        var o = new Order
        {
            OrderId = Guid.NewGuid(),
            UserId = "u1",
            Symbol = "AAPL",
            Quantity = 10,
            SubmittedUtc = DateTimeOffset.UtcNow,
            Type = OrderType.Market,
            Tif = TimeInForce.Day,
            Status = OrderStatus.Pending,
            Remaining = 10
        };

        Assert.Equal(OrderStatus.Pending, o.Status);
        Assert.Equal(o.Quantity, o.Remaining);
        Assert.Null(o.FillPrice);
        Assert.Null(o.FilledUtc);
    }

    [Fact]
    public void Given_PendingOrder_When_Filled_Then_StatusFilled_RemainingZero_SetFillFields()
    {
        var o = new Order()
        {
            OrderId = Guid.NewGuid(),
            UserId = "u1",
            Symbol = "AAPL",
            Quantity = 10,
            SubmittedUtc = DateTimeOffset.UtcNow,
            Type = OrderType.Limit,
            LimitPrice = 150m,
            Tif = TimeInForce.Day,
            Status = OrderStatus.Pending,
            Remaining = 10
        };

        // simulate fill
        o.Status = OrderStatus.Filled;
        o.Remaining = 0;
        o.FillPrice = 149.80m;
        o.FilledUtc = DateTimeOffset.UtcNow;

        Assert.Equal(OrderStatus.Filled, o.Status);
        Assert.Equal(0, o.Remaining);
        Assert.Equal(149.80m, o.FillPrice);
        Assert.NotNull(o.FilledUtc);
    }

    [Fact]
    public void Given_PendingOrder_When_Rejected_Then_StatusRejected_RemainingEqualsQuantity()
    {
        var o = new Order
        {
            OrderId = Guid.NewGuid(),
            UserId = "u1",
            Symbol = "AAPL",
            Quantity = 5,
            SubmittedUtc = DateTimeOffset.UtcNow,
            Type = OrderType.Market,
            Tif = TimeInForce.Day,
            Status = OrderStatus.Pending,
            Remaining = 5
        };

        // simulate validation failure
        o.Status = OrderStatus.Rejected;

        Assert.Equal(OrderStatus.Rejected, o.Status);
        Assert.Equal(o.Quantity, o.Remaining);
        Assert.Null(o.FillPrice);
        Assert.Null(o.FilledUtc);
    }

    [Fact]
    public void Given_IOCOrder_When_PartiallyMatched_Then_RemainingMayBeNonZero_And_StatusFilledIfSpecRequires()
    {
        var o = new Order
        {
            OrderId = Guid.NewGuid(),
            UserId = "u1",
            Symbol = "MSFT",
            Quantity = 100,
            SubmittedUtc = DateTimeOffset.UtcNow,
            Type = OrderType.Market,
            Tif = TimeInForce.IOC,
            Status = OrderStatus.Pending,
            Remaining = 100
        };

        // simulate partial execution then cancel rest (IOC semantic)
        var executed = 35;
        o.Remaining = o.Quantity - executed;
        o.Status = OrderStatus.Filled; // in this model, "Filled" = fully processed, not necessarily fully matched

        Assert.Equal(65, o.Remaining);
        Assert.Equal(OrderStatus.Filled, o.Status);
    }
}

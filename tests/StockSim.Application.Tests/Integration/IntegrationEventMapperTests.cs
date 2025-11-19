using System.Linq;
using System.Text.Json;
using FluentAssertions;
using StockSim.Application.Integration;
using StockSim.Domain.Orders;
using StockSim.Domain.Orders.Events;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Tests.Integration;

public class IntegrationEventMapperTests
{
    private static readonly Guid U = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff");
    private readonly DefaultIntegrationEventMapper _mapper = new();

    [Fact]
    public void OrderAccepted_Maps_To_TradingEnvelope()
    {
        var e = new OrderAccepted(
            U,
            OrderId.New(),
            Symbol.From("AAPL"),
            OrderSide.Buy,
            5m,
            OrderType.Limit,
            100m);

        var ie = _mapper.Map(new[] { e }).Single();

        Assert.Equal("trading.order.accepted", ie.Type);
        Assert.Equal("trading", ie.Source);
        Assert.Equal(e.OrderId.ToString(), ie.Subject);
        Assert.StartsWith("trading|order.accepted|", ie.DedupeKey);

        using var doc = JsonDocument.Parse(ie.Data);
        var root = doc.RootElement;
        Assert.Equal(e.OrderId.ToString(), root.GetProperty("OrderId").GetString());
        Assert.Equal(U, root.GetProperty("UserId").GetGuid());
        Assert.Equal("AAPL", root.GetProperty("Symbol").GetString());
        Assert.Equal("Buy", root.GetProperty("Side").GetString());
        Assert.Equal("Limit", root.GetProperty("Type").GetString());
        Assert.Equal(5m, root.GetProperty("Quantity").GetDecimal());
        Assert.Equal(100m, root.GetProperty("LimitPrice").GetDecimal());
    }

    [Fact]
    public void FundsReserved_Maps_To_PortfolioEnvelope()
    {
        var pid = PortfolioId.New();
        var oid = OrderId.New();
        var e = new StockSim.Domain.Portfolio.Events.FundsReserved(pid, oid, Money.From(250m));

        var ie = _mapper.Map(new[] { e }).Single();

        Assert.Equal("portfolio.funds.reserved", ie.Type);
        Assert.Equal("portfolio", ie.Source);
        Assert.Equal(pid.ToString(), ie.Subject);
        Assert.StartsWith($"portfolio|funds.reserved|{pid}|{oid}", ie.DedupeKey);

        using var doc = JsonDocument.Parse(ie.Data);
        var root = doc.RootElement;
        Assert.Equal(pid.ToString(), root.GetProperty("PortfolioId").GetString());
        Assert.Equal(oid.ToString(), root.GetProperty("OrderId").GetString());
        Assert.Equal(250m, root.GetProperty("Amount").GetDecimal());
    }

    [Fact]
    public void FillApplied_Maps_With_All_Fields()
    {
        var pid = PortfolioId.New();
        var oid = OrderId.New();
        var e = new StockSim.Domain.Portfolio.Events.FillApplied(
            pid, oid, OrderSide.Buy, Symbol.From("MSFT"),
            Quantity.From(3m), Price.From(120m),
            Money.From(-360m), 13m, 100m);

        var ie = _mapper.Map(new[] { e }).Single();

        Assert.Equal("portfolio.fill.applied", ie.Type);
        using var doc = JsonDocument.Parse(ie.Data);
        var root = doc.RootElement;
        Assert.Equal("Buy", root.GetProperty("Side").GetString());
        Assert.Equal("MSFT", root.GetProperty("Symbol").GetString());
        Assert.Equal(3m, root.GetProperty("Quantity").GetDecimal());
        Assert.Equal(120m, root.GetProperty("Price").GetDecimal());
        Assert.Equal(-360m, root.GetProperty("CashDelta").GetDecimal());
        Assert.Equal(13m, root.GetProperty("NewPositionQty").GetDecimal());
        Assert.Equal(100m, root.GetProperty("NewAvgCost").GetDecimal());
    }

    [Fact]
    public void OrderFillApplied_Emits_Versioned_Contracts()
    {
        var oid = OrderId.New();
        var e = new OrderFillApplied(U, oid, Symbol.From("MSFT"), OrderSide.Buy, 2m, 110m, 3m);

        var events = _mapper.Map(new[] { e }).ToArray();
        events.Should().HaveCount(2);

        var v1 = Assert.Single(events, evt => evt.Type == "trading.order.partiallyFilled");
        var v2 = Assert.Single(events, evt => evt.Type == "trading.order.partiallyFilled.v2");

        Assert.Contains("|3", v1.DedupeKey);
        Assert.Equal($"trading|order.partiallyFilled.v2|{oid}|3", v2.DedupeKey);

        using var payload = JsonDocument.Parse(v2.Data);
        var root = payload.RootElement;
        Assert.Equal(oid.ToString(), root.GetProperty("OrderId").GetString());
        Assert.Equal("MSFT", root.GetProperty("Symbol").GetString());
        Assert.Equal("Buy", root.GetProperty("Side").GetString());
        Assert.Equal(2m, root.GetProperty("FillQuantity").GetDecimal());
        Assert.Equal(3m, root.GetProperty("CumFilledQuantity").GetDecimal());
    }

    [Fact]
    public void OrderFillComplete_Emits_V1_And_V2()
    {
        var oid = OrderId.New();
        var e = new OrderFillComplete(U, oid, Symbol.From("TSLA"), OrderSide.Sell, 10m, 250m);

        var events = _mapper.Map(new[] { e }).ToArray();
        events.Should().HaveCount(2);

        var v1 = Assert.Single(events, evt => evt.Type == "trading.order.filled");
        var v2 = Assert.Single(events, evt => evt.Type == "trading.order.filled.v2");

        Assert.Equal($"trading|order.filled|{oid}", v1.DedupeKey);
        Assert.Equal($"trading|order.filled.v2|{oid}", v2.DedupeKey);

        using var payload = JsonDocument.Parse(v2.Data);
        var root = payload.RootElement;
        Assert.Equal("Sell", root.GetProperty("Side").GetString());
        Assert.Equal(10m, root.GetProperty("TotalFilledQuantity").GetDecimal());
        Assert.Equal(250m, root.GetProperty("AverageFillPrice").GetDecimal());
    }
}

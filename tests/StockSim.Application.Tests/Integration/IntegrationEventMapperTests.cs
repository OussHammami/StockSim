using System.Text.Json;
using StockSim.Application.Integration;
using StockSim.Domain.Orders;
using StockSim.Domain.Orders.Events;
using StockSim.Domain.ValueObjects;
using Xunit;

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

        // payload contract is stable
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
}

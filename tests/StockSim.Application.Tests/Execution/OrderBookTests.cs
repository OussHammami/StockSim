using System.Reflection;
using FluentAssertions;
using StockSim.Application.Orders.Execution;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;

public class OrderBookTests
{
    [Fact]
    public void Cross_Returns_Internal_Trade_When_Sides_Overlap()
    {
        var book = new OrderBook();
        var symbol = Symbol.From("AAPL");
        var now = DateTime.UtcNow;

        var bestBid = CreateLimit(symbol, OrderSide.Buy, 5m, 102m, now.AddMinutes(-5));
        var nextBid = CreateLimit(symbol, OrderSide.Buy, 5m, 101m, now.AddMinutes(-4));
        var bestAsk = CreateLimit(symbol, OrderSide.Sell, 3m, 100m, now.AddMinutes(-6));
        var worseAsk = CreateLimit(symbol, OrderSide.Sell, 3m, 105m, now.AddMinutes(-3));

        book.Upsert(nextBid);
        book.Upsert(bestAsk);
        book.Upsert(bestBid);
        book.Upsert(worseAsk);

        var result = book.Cross(symbol, maxLiquidity: 2m);

        result.HasCross.Should().BeTrue();
        result.Cross!.BidOrderId.Should().Be(bestBid.Id);
        result.Cross.AskOrderId.Should().Be(bestAsk.Id);
        result.Cross.Quantity.Should().Be(2m);
        result.Cross.Price.Should().Be((bestBid.LimitPrice!.Value + bestAsk.LimitPrice!.Value) / 2m);
    }

    [Fact]
    public void Remove_Prevents_Further_Cross()
    {
        var book = new OrderBook();
        var symbol = Symbol.From("TSLA");
        var bid = CreateLimit(symbol, OrderSide.Buy, 5m, 105m, DateTime.UtcNow);
        var ask = CreateLimit(symbol, OrderSide.Sell, 5m, 100m, DateTime.UtcNow);

        book.Upsert(bid);
        book.Upsert(ask);

        book.Remove(ask);

        var result = book.Cross(symbol, 5m);
        result.HasCross.Should().BeFalse();
    }

    [Fact]
    public void Cross_Returns_Empty_When_Prices_Do_Not_Overlap()
    {
        var book = new OrderBook();
        var symbol = Symbol.From("NFLX");
        book.Upsert(CreateLimit(symbol, OrderSide.Buy, 5m, 90m, DateTime.UtcNow));
        book.Upsert(CreateLimit(symbol, OrderSide.Sell, 5m, 110m, DateTime.UtcNow));

        var result = book.Cross(symbol, 5m);
        result.HasCross.Should().BeFalse();
    }

    private static Order CreateLimit(Symbol symbol, OrderSide side, decimal qty, decimal price, DateTime created)
    {
        var order = Order.CreateLimit(Guid.NewGuid(), symbol, side, Quantity.From(qty), Price.From(price));
        order.Accept();
        order.ClearDomainEvents();

        typeof(Order).GetProperty(nameof(Order.CreatedAt), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(order, created);

        return order;
    }
}

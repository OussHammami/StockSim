using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
using StockSim.Application.Orders;
using StockSim.Application.Orders.Execution;
using StockSim.Domain.Orders;
using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;

public class TradePrintExecutorTests
{
    [Fact]
    public async Task Print_Fills_Multiple_Buys_In_PriceTime_Priority()
    {
        var repo = Substitute.For<IOrderRepository>();
        var dispatcher = Substitute.For<IEventDispatcher>();
        var mapper = Substitute.For<IIntegrationEventMapper>();
        var outbox = Substitute.For<IOutboxWriter<ITradingOutboxContext>>();

        var o1 = CreateLimitOrder("META", OrderSide.Buy, 10m, 100m, DateTime.UtcNow.AddMinutes(-2));
        var o2 = CreateLimitOrder("META", OrderSide.Buy, 15m, 101m, DateTime.UtcNow.AddMinutes(-1));
        var o3 = CreateLimitOrder("META", OrderSide.Buy, 20m, 99m, DateTime.UtcNow);

        repo.GetOpenBySymbolAsync(Symbol.From("META"), Arg.Any<CancellationToken>())
            .Returns(new List<Order> { o1, o2, o3 });

        var exec = new TradePrintExecutor(repo, dispatcher, mapper, outbox, NullLogger<TradePrintExecutor>.Instance);
        var print = new TradePrint("META", 100m, 25m, TradeAggressor.Unknown, DateTimeOffset.UtcNow);

        await exec.ExecuteAsync(print);

        o2.FilledQuantity.Should().Be(15m);
        o1.FilledQuantity.Should().Be(10m);
        o3.FilledQuantity.Should().Be(0m);
    }

    [Fact]
    public async Task Print_Fills_Both_Sides_With_PriceTime_Priority()
    {
        var repo = Substitute.For<IOrderRepository>();
        var dispatcher = Substitute.For<IEventDispatcher>();
        var mapper = Substitute.For<IIntegrationEventMapper>();
        mapper.Map(Arg.Any<IEnumerable<IDomainEvent>>()).Returns(Array.Empty<IntegrationEvent>());
        var outbox = Substitute.For<IOutboxWriter<ITradingOutboxContext>>();

        var buyEarly = CreateLimitOrder("META", OrderSide.Buy, 10m, 101m, DateTime.UtcNow.AddMinutes(-5));
        var buyLate = CreateLimitOrder("META", OrderSide.Buy, 5m, 101m, DateTime.UtcNow.AddMinutes(-1));

        var sellBest = CreateLimitOrder("META", OrderSide.Sell, 5m, 100m, DateTime.UtcNow.AddMinutes(-4));
        var sellWorse = CreateLimitOrder("META", OrderSide.Sell, 10m, 101m, DateTime.UtcNow.AddMinutes(-2));

        repo.GetOpenBySymbolAsync(Symbol.From("META"), Arg.Any<CancellationToken>())
            .Returns(new List<Order> { buyLate, sellBest, sellWorse, buyEarly });

        var exec = new TradePrintExecutor(repo, dispatcher, mapper, outbox, NullLogger<TradePrintExecutor>.Instance);
        var print = new TradePrint("META", 101m, 15m, TradeAggressor.Unknown, DateTimeOffset.UtcNow);

        await exec.ExecuteAsync(print);

        buyEarly.FilledQuantity.Should().Be(10m);
        buyLate.FilledQuantity.Should().Be(5m);
        sellBest.FilledQuantity.Should().Be(5m);
        sellWorse.FilledQuantity.Should().Be(10m);
    }

    [Fact]
    public async Task ExecuteAsync_Emits_Domain_And_Integration_Events()
    {
        var repo = Substitute.For<IOrderRepository>();
        var dispatcher = Substitute.For<IEventDispatcher>();
        var mapper = Substitute.For<IIntegrationEventMapper>();
        var outbox = Substitute.For<IOutboxWriter<ITradingOutboxContext>>();

        var buy = CreateLimitOrder("AAPL", OrderSide.Buy, 5m, 100m, DateTime.UtcNow.AddMinutes(-1));
        var sell = CreateLimitOrder("AAPL", OrderSide.Sell, 5m, 100m, DateTime.UtcNow);

        repo.GetOpenBySymbolAsync(Symbol.From("AAPL"), Arg.Any<CancellationToken>())
            .Returns(new[] { buy, sell });

        IEnumerable<IDomainEvent>? capturedEvents = null;
        var integrationEvents = new[]
        {
            IntegrationEvent.Create("evt", "src", buy.Id.ToString(), new { side = "buy" }, DateTimeOffset.UtcNow),
            IntegrationEvent.Create("evt", "src", sell.Id.ToString(), new { side = "sell" }, DateTimeOffset.UtcNow),
        };

        mapper.Map(Arg.Any<IEnumerable<IDomainEvent>>())
            .Returns(call =>
            {
                capturedEvents = call.Arg<IEnumerable<IDomainEvent>>();
                return integrationEvents;
            });

        var exec = new TradePrintExecutor(repo, dispatcher, mapper, outbox, NullLogger<TradePrintExecutor>.Instance);
        var print = new TradePrint("AAPL", 100m, 5m, TradeAggressor.Unknown, DateTimeOffset.UtcNow);

        await exec.ExecuteAsync(print);

        capturedEvents.Should().NotBeNull();
        capturedEvents!.Should().HaveCount(4); // each order raises applied + complete

        await dispatcher.Received(1)
            .DispatchAsync(Arg.Is<IEnumerable<IDomainEvent>>(evts => evts.SequenceEqual(capturedEvents!)), Arg.Any<CancellationToken>());

        await outbox.Received(1)
            .WriteAsync(Arg.Is<IEnumerable<IntegrationEvent>>(list => list.SequenceEqual(integrationEvents)), Arg.Any<CancellationToken>());

        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Uses_Slippage_Model_Output()
    {
        var repo = Substitute.For<IOrderRepository>();
        var dispatcher = Substitute.For<IEventDispatcher>();
        var mapper = Substitute.For<IIntegrationEventMapper>();
        mapper.Map(Arg.Any<IEnumerable<IDomainEvent>>()).Returns(Array.Empty<IntegrationEvent>());
        var outbox = Substitute.For<IOutboxWriter<ITradingOutboxContext>>();
        var slippage = Substitute.For<ISlippageModel>();
        slippage.AdjustPrice(Arg.Any<decimal>(), Arg.Any<decimal>(), Arg.Any<QuoteSnapshot>()).Returns(123.45m);

        var order = CreateLimitOrder("TSLA", OrderSide.Buy, 2m, 150m, DateTime.UtcNow);
        repo.GetOpenBySymbolAsync(Symbol.From("TSLA"), Arg.Any<CancellationToken>())
            .Returns(new[] { order });

        var exec = new TradePrintExecutor(repo, dispatcher, mapper, outbox, NullLogger<TradePrintExecutor>.Instance, slippage);
        var print = new TradePrint("TSLA", 150m, 2m, TradeAggressor.Unknown, DateTimeOffset.UtcNow);

        await exec.ExecuteAsync(print);

        slippage.Received(1).AdjustPrice(150m, 2m, Arg.Any<QuoteSnapshot>());
        order.AverageFillPrice.Should().Be(123.45m);
    }

    private static Order CreateLimitOrder(string symbol, OrderSide side, decimal qty, decimal price, DateTime createdAt)
    {
        var order = Order.CreateLimit(Guid.NewGuid(), Symbol.From(symbol), side, Quantity.From(qty), Price.From(price));
        order.Accept();
        order.ClearDomainEvents();

        typeof(Order).GetProperty(nameof(Order.CreatedAt), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(order, createdAt);

        return order;
    }
}

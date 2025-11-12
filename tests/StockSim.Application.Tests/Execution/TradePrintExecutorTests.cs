using FluentAssertions;
using NSubstitute;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
using StockSim.Application.Orders;
using StockSim.Application.Orders.Execution;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;
using Xunit;

public class TradePrintExecutorTests
{
    [Fact]
    public async Task Print_Fills_Multiple_Buys_In_PriceTime_Priority()
    {
        // Arrange
        var repo = Substitute.For<IOrderRepository>();
        var dispatcher = Substitute.For<IEventDispatcher>();
        var mapper = Substitute.For<IIntegrationEventMapper>();
        var outbox = Substitute.For<IOutboxWriter<ITradingOutboxContext>>();

        var o1 = Order.CreateLimit(Guid.NewGuid(), Symbol.From("META"), OrderSide.Buy, Quantity.From(10), Price.From(100)); o1.Accept();
        var o2 = Order.CreateLimit(Guid.NewGuid(), Symbol.From("META"), OrderSide.Buy, Quantity.From(15), Price.From(101)); o2.Accept();
        var o3 = Order.CreateLimit(Guid.NewGuid(), Symbol.From("META"), OrderSide.Buy, Quantity.From(20), Price.From(99)); o3.Accept();

        repo.GetOpenBySymbolAsync(Symbol.From("META"), default).Returns(new List<Order> { o1, o2, o3 });

        var exec = new TradePrintExecutor(repo, dispatcher, mapper, outbox);
        var print = new TradePrint("META", 100m, 25m, TradeAggressor.Unknown, DateTimeOffset.UtcNow);

        // Act
        await exec.ExecuteAsync(print);

        // Assert
        o2.FilledQuantity.Should().Be(15m);   // higher price priority
        o1.FilledQuantity.Should().Be(10m);   // then 100 @ 10
        o3.FilledQuantity.Should().Be(0m);    // 99 not eligible for trade at 100 (actually it is eligible because limit >= price? For buy, we require limit >= price, 99 >= 100 is false → correct.)
    }
}
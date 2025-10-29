using Microsoft.Extensions.DependencyInjection;
using StockSim.Application;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Integration;
using StockSim.Application.Orders.Handlers;
using StockSim.Application.Portfolios;
using StockSim.Application.Tests.Fakes;
using StockSim.Domain.Orders;
using StockSim.Domain.Orders.Events;
using StockSim.Domain.Portfolio;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Tests.Events;
public class EventDispatcherTests
{
    private static readonly Guid U = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff");

    [Fact]
    public async Task OrderAccepted_BuyLimit_Reserves_Funds()
    {
        // arrange DI
        var svcs = new ServiceCollection();
        svcs.AddApplicationCore();
        var repo = new InMemoryPortfolioRepository();
        var outbox = new InMemoryOutboxWriter();
        svcs.AddSingleton<IPortfolioRepository>(repo);
        svcs.AddSingleton<IDomainEventHandler<OrderAccepted>, OrderAcceptedHandler>();
        svcs.AddSingleton<IOutboxWriter>(outbox);
        var sp = svcs.BuildServiceProvider();

        // seed portfolio
        var p = new Portfolio(PortfolioId.New(), U);
        p.Deposit(Money.From(1000m));
        repo.Seed(p);

        var dispatcher = sp.GetRequiredService<IEventDispatcher>();

        // act
        var evt = new OrderAccepted(U, OrderId.New(), Symbol.From("AAPL"), OrderSide.Buy, 5m, OrderType.Limit, 100m);
        await dispatcher.DispatchAsync(new[] { evt });

        // assert
        var loaded = await repo.GetByUserAsync(U);
        Assert.Equal(500m, loaded!.ReservedCash.Amount);
        Assert.Equal(500m, loaded.AvailableCash().Amount);
    }

    [Fact]
    public async Task OrderAccepted_Sell_Reserves_Shares()
    {
        var svcs = new ServiceCollection();
        svcs.AddApplicationCore();
        var repo = new InMemoryPortfolioRepository();
        var outbox = new InMemoryOutboxWriter();
        svcs.AddSingleton<IPortfolioRepository>(repo);
        svcs.AddSingleton<IDomainEventHandler<OrderAccepted>, OrderAcceptedHandler>();
        svcs.AddSingleton<IOutboxWriter>(outbox);
        var sp = svcs.BuildServiceProvider();

        var sym = Symbol.From("MSFT");
        var p = new Portfolio(PortfolioId.New(), U);
        p.Deposit(Money.From(1000m));
        // own 10 @ 100
        p.ApplyFill(OrderId.New(), OrderSide.Buy, sym, Quantity.From(10), Price.From(100));
        repo.Seed(p);

        var dispatcher = sp.GetRequiredService<IEventDispatcher>();
        var evt = new OrderAccepted(U, OrderId.New(), sym, OrderSide.Sell, 4m, OrderType.Limit, 0m);
        await dispatcher.DispatchAsync(new[] { evt });

        var loaded = await repo.GetByUserAsync(U);
        Assert.Equal(4m, loaded!.ReservedFor(sym));
    }
}

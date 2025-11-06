using Microsoft.Extensions.DependencyInjection;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
using StockSim.Application.Orders;
using StockSim.Application.Orders.Commands;
using StockSim.Application.Orders.Handlers;
using StockSim.Application.Portfolios;
using StockSim.Application.Tests.Fakes;
using StockSim.Domain.Portfolio;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Tests.Orders;

public class OrderServiceTests
{
    private static readonly Guid U = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff");

    [Fact]
    public async Task Place_Limit_Buy_Reserves_Funds()
    {
        var svcs = new ServiceCollection();
        svcs.AddApplicationCore();
        var orders = new InMemoryOrderRepository();
        var outbox = new InMemoryOutboxWriter();
        var portfolios = new InMemoryPortfolioRepository();
        var p = new Portfolio(PortfolioId.New(), U);
        p.Deposit(Money.From(1000m));
        portfolios.Seed(p);

        svcs.AddSingleton<IOrderRepository>(orders);
        svcs.AddSingleton<IPortfolioRepository>(portfolios);
        svcs.AddSingleton<IDomainEventHandler<Domain.Orders.Events.OrderAccepted>, OrderAcceptedHandler>();
        svcs.AddSingleton<IOutboxWriter<IPortfolioOutboxContext>>(outbox);

        var sp = svcs.BuildServiceProvider();
        var svc = sp.GetRequiredService<IOrderService>();

        var id = await svc.PlaceAsync(new PlaceOrder(U, "AAPL", Domain.Orders.OrderSide.Buy, Domain.Orders.OrderType.Limit, 5m, 100m));

        var loaded = await portfolios.GetByUserAsync(U);
        Assert.Equal(500m, loaded!.ReservedCash.Amount);
    }
}

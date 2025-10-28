using Microsoft.Extensions.DependencyInjection;
using StockSim.Application;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Orders;
using StockSim.Application.Orders.Commands;
using StockSim.Application.Orders.Handlers;
using StockSim.Application.Portfolios;
using StockSim.Application.Tests.Fakes;
using StockSim.Domain.ValueObjects;
using Xunit;

public class OrderServiceTests
{
    private static readonly Guid U = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff");

    [Fact]
    public async Task Place_Limit_Buy_Reserves_Funds()
    {
        var svcs = new ServiceCollection();
        svcs.AddApplicationCore();
        var orders = new InMemoryOrderRepository();
        var portfolios = new InMemoryPortfolioRepository();
        var p = new StockSim.Domain.Portfolio.Portfolio(PortfolioId.New(), U);
        p.Deposit(StockSim.Domain.ValueObjects.Money.From(1000m));
        portfolios.Seed(p);

        svcs.AddSingleton<IOrderRepository>(orders);
        svcs.AddSingleton<IPortfolioRepository>(portfolios);
        svcs.AddSingleton<IDomainEventHandler<StockSim.Domain.Orders.Events.OrderAccepted>, OrderAcceptedHandler>();

        var sp = svcs.BuildServiceProvider();
        var svc = sp.GetRequiredService<IOrderService>();

        var id = await svc.PlaceAsync(new PlaceOrder(U, "AAPL", StockSim.Domain.Orders.OrderSide.Buy, StockSim.Domain.Orders.OrderType.Limit, 5m, 100m));

        var loaded = await portfolios.GetByUserAsync(U);
        Assert.Equal(500m, loaded!.ReservedCash.Amount);
    }
}

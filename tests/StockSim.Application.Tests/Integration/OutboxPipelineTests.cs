using Microsoft.Extensions.DependencyInjection;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Integration;
using StockSim.Application.Orders;
using StockSim.Application.Orders.Commands;
using StockSim.Application.Orders.Handlers;
using StockSim.Application.Portfolios;
using StockSim.Application.Tests.Fakes;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Tests.Integration;

public class OutboxPipelineTests
{
    private static readonly Guid U = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff");

    [Fact]
    public async Task Place_Limit_Buy_Writes_OrderAccepted_And_FundsReserved_Outbox_Events()
    {
        var svcs = new ServiceCollection();
        svcs.AddApplicationCore();
        var orders = new InMemoryOrderRepository();
        var portfolios = new InMemoryPortfolioRepository();
        var outbox = new InMemoryOutboxWriter();

        var p = new StockSim.Domain.Portfolio.Portfolio(PortfolioId.New(), U);
        p.Deposit(Money.From(1000m));
        portfolios.Seed(p);

        svcs.AddSingleton<IOrderRepository>(orders);
        svcs.AddSingleton<IPortfolioRepository>(portfolios);
        svcs.AddSingleton<IDomainEventHandler<StockSim.Domain.Orders.Events.OrderAccepted>, OrderAcceptedHandler>();
        svcs.AddSingleton<IOutboxWriter>(outbox);

        var sp = svcs.BuildServiceProvider();
        var svc = sp.GetRequiredService<IOrderService>();

        await svc.PlaceAsync(new PlaceOrder(U, "AAPL", StockSim.Domain.Orders.OrderSide.Buy, StockSim.Domain.Orders.OrderType.Limit, 5m, 100m));

        Assert.Contains(outbox.Items, e => e.Type == "trading.order.accepted");
        Assert.Contains(outbox.Items, e => e.Type == "portfolio.funds.reserved");
    }
}

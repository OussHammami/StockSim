using FluentAssertions;
using NSubstitute;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
using StockSim.Application.Orders;
using StockSim.Application.Orders.Commands;
using StockSim.Domain.Orders;
using StockSim.Domain.Primitives;
using StockSim.Domain.ValueObjects;
using Xunit;

namespace StockSim.Application.Tests.Orders;

public class OrderServiceTests
{
    private readonly IOrderRepository _repo = Substitute.For<IOrderRepository>();
    private readonly IEventDispatcher _events = Substitute.For<IEventDispatcher>();
    private readonly IIntegrationEventMapper _mapper = Substitute.For<IIntegrationEventMapper>();
    private readonly IOutboxWriter<ITradingOutboxContext> _outbox = Substitute.For<IOutboxWriter<ITradingOutboxContext>>();

    [Fact]
    public async Task PlaceAsync_Persists_Dispatches_Maps_WritesOutbox_Saves()
    {
        var svc = new OrderService(_repo, _events, _mapper, _outbox);
        var cmd = new PlaceOrder(Guid.NewGuid(), "AAPL", OrderSide.Buy, OrderType.Market, 5m, null);

        var id = await svc.PlaceAsync(cmd);

        id.Should().NotBeNull();
        await _repo.Received(1).AddAsync(Arg.Any<Order>());
        await _events.Received(1).DispatchAsync(Arg.Any<IEnumerable<IDomainEvent>>(), default);
        _mapper.Received(1).Map(Arg.Any<IEnumerable<IDomainEvent>>());
        await _outbox.Received(1).WriteAsync(Arg.Any<IEnumerable<IntegrationEvent>>(), default);
        await _repo.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task CancelAsync_Loads_Order_And_WritesOutbox()
    {
        var order = Order.CreateMarket(Guid.NewGuid(), Symbol.From("MSFT"), OrderSide.Sell, Quantity.From(2));
        order.Accept();

        _repo.GetAsync(order.Id, default).Returns(order);
        var svc = new OrderService(_repo, _events, _mapper, _outbox);

        await svc.CancelAsync(new CancelOrder(order.UserId, order.Id, "user canceled"));

        await _events.Received(1).DispatchAsync(Arg.Any<IEnumerable<IDomainEvent>>(), default);
        await _outbox.Received(1).WriteAsync(Arg.Any<IEnumerable<IntegrationEvent>>(), default);
        await _repo.Received(1).SaveChangesAsync(default);
    }
}
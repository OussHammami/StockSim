using FluentAssertions;
using NSubstitute;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
using StockSim.Application.Orders;
using StockSim.Application.Orders.Commands;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;
using Xunit;

namespace StockSim.Application.Tests.Orders;

public class OrderServiceEdgeCasesTests
{
    private readonly IOrderRepository _repo = Substitute.For<IOrderRepository>();
    private readonly IEventDispatcher _events = Substitute.For<IEventDispatcher>();
    private readonly IIntegrationEventMapper _mapper = Substitute.For<IIntegrationEventMapper>();
    private readonly IOutboxWriter<ITradingOutboxContext> _outbox = Substitute.For<IOutboxWriter<ITradingOutboxContext>>();

    [Fact]
    public async Task CancelAsync_When_Order_NotFound_Throws()
    {
        _repo.GetAsync(Arg.Any<OrderId>()).Returns((Order?)null);

        var svc = new OrderService(_repo, _events, _mapper, _outbox);

        Func<Task> act = async () => await svc.CancelAsync(new CancelOrder(Guid.NewGuid(), OrderId.From(Guid.NewGuid()), "nope"));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PlaceAsync_Limit_Requires_Price()
    {
        var svc = new OrderService(_repo, _events, _mapper, _outbox);
        var cmd = new PlaceOrder(Guid.NewGuid(), "AAPL", OrderSide.Buy, OrderType.Limit, 1m, null);

        Func<Task> act = async () => await svc.PlaceAsync(cmd);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
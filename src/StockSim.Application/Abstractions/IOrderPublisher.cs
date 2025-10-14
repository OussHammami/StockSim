using StockSim.Application.Contracts.Orders;

namespace StockSim.Application.Abstractions;

public interface IOrderPublisher
{
    void Publish(OrderCommand cmd);
}
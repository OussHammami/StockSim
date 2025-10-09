using StockSim.Application.Contracts.Orders;

public interface IOrderCommands
{
    Task PlaceAsync(OrderCommand cmd, CancellationToken ct = default);
}

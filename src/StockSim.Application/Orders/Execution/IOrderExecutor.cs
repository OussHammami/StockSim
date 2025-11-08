namespace StockSim.Application.Orders.Execution;

public interface IOrderExecutor
{
    Task ExecuteForSymbolAsync(string symbol, CancellationToken ct = default);
    Task SweepAllAsync(CancellationToken ct = default);
}
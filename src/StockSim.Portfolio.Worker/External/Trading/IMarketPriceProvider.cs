namespace StockSim.Portfolio.Worker.External.Trading;

public interface IMarketPriceProvider
{
    Task<decimal?> GetAskAsync(string symbol, CancellationToken ct = default);
}
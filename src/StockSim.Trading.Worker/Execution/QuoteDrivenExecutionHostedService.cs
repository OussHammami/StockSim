using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockSim.Application.Orders.Execution;

public sealed class QuoteDrivenExecutionHostedService : IHostedService
{
    private readonly ILogger<QuoteDrivenExecutionHostedService> _log;
    private readonly IOrderExecutor _executor;
    private readonly IQuoteStream _stream;
    private IDisposable? _sub;

    public QuoteDrivenExecutionHostedService(
        ILogger<QuoteDrivenExecutionHostedService> log,
        IOrderExecutor executor,
        IQuoteStream stream)
    {
        _log = log;
        _executor = executor;
        _stream = stream;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _sub = _stream.Subscribe(async q =>
        {
            try
            {
                await _executor.ExecuteForSymbolAsync(q.Symbol, ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Execution failed for {Symbol}", q.Symbol);
            }
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _sub?.Dispose();
        return Task.CompletedTask;
    }
}

public interface IQuoteStream
{
    IDisposable Subscribe(Func<QuoteMsg, Task> onQuote);
}

public sealed record QuoteMsg(string Symbol, decimal Bid, decimal Ask, decimal? Last, DateTimeOffset Ts);
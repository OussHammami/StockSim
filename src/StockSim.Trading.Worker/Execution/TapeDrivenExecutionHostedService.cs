using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockSim.Application.Orders.Execution;

namespace StockSim.Trading.Worker.Execution;

/// <summary>
/// Subscribes to trade prints and executes them immediately.
/// </summary>
public sealed class TapeDrivenExecutionHostedService : IHostedService
{
    private readonly ILogger<TapeDrivenExecutionHostedService> _log;
    private readonly ITradePrintStream _tape;
    private readonly TradePrintExecutor _executor;
    private IDisposable? _sub;

    public TapeDrivenExecutionHostedService(
        ILogger<TapeDrivenExecutionHostedService> log,
        ITradePrintStream tape,
        TradePrintExecutor executor)
    {
        _log = log;
        _tape = tape;
        _executor = executor;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _sub = _tape.Subscribe(async p =>
        {
            try
            {
                await _executor.ExecuteAsync(p, ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Trade print execution failed for {Symbol} {Qty}@{Price}", p.Symbol, p.Quantity, p.Price);
            }
        });
        _log.LogInformation("Tape-driven execution started.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _sub?.Dispose();
        _log.LogInformation("Tape-driven execution stopped.");
        return Task.CompletedTask;
    }
}
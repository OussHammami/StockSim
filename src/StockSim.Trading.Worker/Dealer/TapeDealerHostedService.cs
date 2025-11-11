using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using StockSim.Application.Orders.Execution;

namespace StockSim.Trading.Worker.Dealer;

/// <summary>
/// Simulates real-market prints (tape). Later, replace with a real market adapter
/// that pushes TradePrints via ITradePrintStream.
/// </summary>
public sealed class TapeDealerHostedService : BackgroundService, ITradePrintStream
{
    private readonly ILogger<TapeDealerHostedService> _log;
    private readonly IConfiguration _cfg;
    private readonly IQuoteSnapshotProvider _quotes;

    private readonly List<Func<TradePrint, Task>> _subs = new();
    private readonly string[] _symbols;
    private readonly TimeSpan _interval;
    private readonly decimal _meanSize;
    private readonly Random _rng = new();

    public TapeDealerHostedService(ILogger<TapeDealerHostedService> log, IConfiguration cfg, IQuoteSnapshotProvider quotes)
    {
        _log = log;
        _cfg = cfg;
        _quotes = quotes;

        _symbols = _cfg.GetValue<string>("TAPE__SYMBOLS")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   ?? new[] { "AAPL", "MSFT", "AMZN", "GOOGL", "NVDA", "TSLA", "META" };
        _interval = TimeSpan.FromMilliseconds(_cfg.GetValue<int?>("TAPE__INTERVAL_MS") ?? 400);
        _meanSize = _cfg.GetValue<decimal?>("TAPE__MEAN_SIZE") ?? 25m; // average trade size
    }

    public IDisposable Subscribe(Func<TradePrint, Task> onPrint)
    {
        _subs.Add(onPrint);
        return new Sub(_subs, onPrint);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Tape dealer started: symbols={Symbols}, interval={Interval}ms", string.Join(",", _symbols), _interval.TotalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var sym in _symbols)
                {
                    var snap = _quotes.Get(sym);
                    if (snap is null) continue;

                    var side = _rng.NextDouble() < 0.5 ? TradeAggressor.Buyer : TradeAggressor.Seller;
                    var price = side == TradeAggressor.Buyer ? snap.Ask : snap.Bid;
                    if (price <= 0) price = (snap.Bid + snap.Ask) / 2m;

                    // Sample a size around mean (positive)
                    var size = Math.Max(1m, (decimal)Math.Round(NormalSample((double)_meanSize, (double)(_meanSize / 3)), MidpointRounding.AwayFromZero));

                    var print = new TradePrint(sym, price, size, side, DateTimeOffset.UtcNow);
                    foreach (var sub in _subs.ToArray())
                        await sub(print);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Tape dealer tick failed.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private static double NormalSample(double mean, double stddev)
    {
        // Box-Muller
        var u1 = 1.0 - Random.Shared.NextDouble();
        var u2 = 1.0 - Random.Shared.NextDouble();
        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stddev * randStdNormal;
    }

    private sealed class Sub : IDisposable
    {
        private readonly List<Func<TradePrint, Task>> _subs;
        private readonly Func<TradePrint, Task> _cb;
        public Sub(List<Func<TradePrint, Task>> subs, Func<TradePrint, Task> cb) { _subs = subs; _cb = cb; }
        public void Dispose() => _subs.Remove(_cb);
    }
}
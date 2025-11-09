using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockSim.Application.Orders;
using StockSim.Application.Orders.Commands;
using StockSim.Application.Orders.Execution;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;

namespace StockSim.Trading.Worker.Dealer;

public sealed class DealerQuoterHostedService : BackgroundService
{
    private readonly ILogger<DealerQuoterHostedService> _log;
    private readonly IOrderService _orders;
    private readonly IQuoteSnapshotProvider _quotes;

    // Synthetic dealer user id (constant just for demo)
    private static readonly Guid DealerUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private readonly string[] _symbols = new[] { "AAPL", "MSFT", "TSLA" };
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(3);
    private const decimal DealerQty = 50m;   // post 50 shares on each side
    private const decimal Tick = 0.05m;      // 5c tick spread around mid

    public DealerQuoterHostedService(ILogger<DealerQuoterHostedService> log, IOrderService orders, IQuoteSnapshotProvider quotes)
    {
        _log = log;
        _orders = orders;
        _quotes = quotes;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Dealer quoter started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var sym in _symbols)
                {
                    var snap = _quotes.Get(sym);
                    if (snap is null) continue;

                    // compute mid
                    var mid = (snap.Bid + snap.Ask) / 2m;
                    var bid = mid - Tick;
                    var ask = mid + Tick;

                    // Post or refresh dealer quotes (simplification: just place fresh orders)
                    await _orders.PlaceAsync(new PlaceOrder(
                        DealerUserId, sym, OrderSide.Buy, OrderType.Limit, DealerQty, bid), stoppingToken);

                    await _orders.PlaceAsync(new PlaceOrder(
                        DealerUserId, sym, OrderSide.Sell, OrderType.Limit, DealerQty, ask), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Dealer quoter tick failed.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
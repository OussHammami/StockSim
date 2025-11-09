using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockSim.Application.Orders;
using StockSim.Domain.Orders;

public sealed class OrderMaintenanceHostedService : BackgroundService
{
    private readonly ILogger<OrderMaintenanceHostedService> _log;
    private readonly IOrderRepository _orders;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(15);

    public OrderMaintenanceHostedService(ILogger<OrderMaintenanceHostedService> log, IOrderRepository orders)
    {
        _log = log;
        _orders = orders;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // TODO: add repository method to get all open orders (maybe by symbol list)
                // For brevity assume a method GetAllOpenAsync exists. Otherwise implement.
                // var open = await _orders.GetAllOpenAsync(stoppingToken);

                // Pseudo:
                // foreach (var o in open)
                // {
                //     if (o.IsExpired(DateTimeOffset.UtcNow))
                //         o.Cancel("expired");
                //     if (o.TimeInForce == TimeInForce.Ioc && o.State == OrderState.Accepted)
                //         o.Cancel("IOC not filled");
                //     if (o.TimeInForce == TimeInForce.Fok && o.State == OrderState.Accepted)
                //         o.Cancel("FOK not fully filled");
                // }
                // await _orders.SaveChangesAsync(stoppingToken);

                // Implementation note: create repository method for efficiency later.
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Maintenance sweep failed.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
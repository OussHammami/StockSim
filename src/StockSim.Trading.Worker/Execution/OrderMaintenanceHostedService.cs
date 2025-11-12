using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockSim.Application.Orders;
using StockSim.Domain.Orders;

namespace StockSim.Trading.Worker.Execution;

public sealed class OrderMaintenanceHostedService : BackgroundService
{
    private readonly ILogger<OrderMaintenanceHostedService> _log;
    private readonly IConfiguration _cfg;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly TimeSpan _interval;
    private readonly TimeSpan _staleAcceptedAge;

    public OrderMaintenanceHostedService(
        ILogger<OrderMaintenanceHostedService> log,
        IConfiguration cfg,
        IServiceScopeFactory scopeFactory)
    {
        _log = log;
        _cfg = cfg;
        _scopeFactory = scopeFactory;

        _interval = TimeSpan.FromSeconds(_cfg.GetValue<int?>("MAINT__INTERVAL_SECONDS") ?? 15);
        _staleAcceptedAge = TimeSpan.FromSeconds(_cfg.GetValue<int?>("MAINT__STALE_ACCEPTED_SECONDS") ?? 60);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Order maintenance started. Interval={Interval}s, StaleAccepted={Stale}s",
            _interval.TotalSeconds, _staleAcceptedAge.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var now = DateTimeOffset.UtcNow;

                var open = await repo.GetAllOpenAsync(stoppingToken).ConfigureAwait(false);
                var modified = 0;

                foreach (var o in open)
                {
                    // IOC remainder: if still open, cancel it
                    if (o.TimeInForce == TimeInForce.Ioc &&
                        (o.State == OrderState.Accepted || o.State == OrderState.PartiallyFilled))
                    {
                        o.Cancel("IOC remainder canceled");
                        modified++;
                        continue;
                    }

                    // DAY expiry: naive check using date rollover if ExpiresAt not available
                    var expires = GetExpiry(o);
                    if (expires.HasValue && now >= expires.Value &&
                        (o.State == OrderState.Accepted || o.State == OrderState.PartiallyFilled))
                    {
                        o.Cancel("DAY expired");
                        modified++;
                        continue;
                    }

                    // Zombie accepted: cancel after a grace period if still not filled
                    if (o.State == OrderState.Accepted &&
                        now - o.CreatedAt >= _staleAcceptedAge)
                    {
                        o.Cancel("stale accepted canceled");
                        modified++;
                        continue;
                    }
                }

                if (modified > 0)
                {
                    await repo.SaveChangesAsync(stoppingToken).ConfigureAwait(false);
                    _log.LogInformation("Maintenance: canceled {Count} orders.", modified);
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Order maintenance sweep failed.");
            }

            try { await Task.Delay(_interval, stoppingToken); } catch { /* ignore */ }
        }
    }

    private static DateTimeOffset? GetExpiry(dynamic order)
    {
        // Prefer ExpiresAt if present on the aggregate (TimeInForce.Day path in domain)
        try
        {
            DateTimeOffset? expiresAt = order.ExpiresAt;
            if (expiresAt.HasValue) return expiresAt;
        }
        catch { /* property may not exist */ }

        // Fallback: end of order's UTC day
        if (order.TimeInForce == TimeInForce.Day)
        {
            var c = order.CreatedAt;
            return new DateTimeOffset(c.Year, c.Month, c.Day, 23, 59, 59, TimeSpan.Zero);
        }
        return null;
    }
}
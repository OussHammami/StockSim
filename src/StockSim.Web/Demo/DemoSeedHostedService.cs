using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using StockSim.Application.Orders;
using StockSim.Application.Orders.Commands;
using StockSim.Application.Portfolios;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;

namespace StockSim.Web.Demo;

public sealed class DemoSeedHostedService(
    IServiceScopeFactory scopes,
    IOptions<DemoSeedOptions> options,
    ILogger<DemoSeedHostedService> log) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var opt = options.Value;
        if (!opt.Enabled)
        {
            log.LogInformation("Demo seed disabled by configuration.");
            return;
        }

        using var scope = scopes.CreateScope();
        var portfolios = scope.ServiceProvider.GetRequiredService<IPortfolioService>();
        var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var userId = StableGuid(opt.DemoUserSubject);

        // Idempotent: ensure portfolio exists and deposit once if needed
        var p = await portfolios.GetOrCreateAsync(userId, ct);
        if (p.Cash.Amount < opt.InitialCash)
        {
            var delta = opt.InitialCash - p.Cash.Amount;
            if (delta > 0)
            {
                log.LogInformation("Depositing {Amount} to demo user {User}", delta, userId);
                await portfolios.DepositAsync(userId, Money.From(delta), ct);
            }
        }

        // Place a few sample limit orders if none exist yet
        var existing = await orders.GetByUserAsync(userId, ct); // implement or replace with your query
        if (existing.Count == 0)
        {
            foreach (var s in opt.Symbols)
            {
                var cmd = new PlaceOrder(
                    UserId: userId,
                    Symbol: s,
                    Side: OrderSide.Buy,
                    Type: OrderType.Limit,
                    Quantity: opt.DefaultQty,
                    LimitPrice: opt.DefaultLimit);

                try
                {
                    var id = await orders.PlaceAsync(cmd, ct);
                    log.LogInformation("Seeded order {OrderId} {Side} {Qty} {Symbol} @ {Price}",
                        id, cmd.Side, cmd.Quantity, cmd.Symbol, cmd.LimitPrice);
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Skipping seed order for {Symbol}", s);
                }
            }
        }
        else
        {
            log.LogInformation("Skipping order seeds. {Count} existing orders for demo user.", existing.Count);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private static Guid StableGuid(string subject)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(subject));
        Span<byte> g = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(g);
        return new Guid(g);
    }
}

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using StockSim.Application.Abstractions;
using StockSim.Application.Contracts.Orders;
using StockSim.Domain.Enums;
using StockSim.Domain.Models;
using StockSim.Infrastructure.Persistence;
using StockSim.Infrastructure.Persistence.Entities;

namespace StockSim.Web.Services;

// Listens to MarketFeed quotes and attempts to fill crossing Limit orders at limit-or-better.
public sealed class QuoteMatcherService(IServiceProvider sp, IConfiguration cfg, ILogger<QuoteMatcherService> logger) : BackgroundService
{
    private HubConnection? _quotesHub;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var baseUrl = cfg["MarketFeed:BaseUrl"] ?? "http://localhost:8081";
        _quotesHub = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/quotes")
            .WithAutomaticReconnect()
            .Build();

        _quotesHub.On<Quote>("quote", async q =>
        {
            try
            {
                using var scope = sp.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<LastQuotesCache>();
                cache.Upsert(q);

                await TryMatchSymbolAsync(q.Symbol, q.Price, scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error processing quote for {Symbol}", q.Symbol);
            }
        });

        // Connect and stay connected
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _quotesHub.StartAsync(stoppingToken);
                logger.LogInformation("QuoteMatcherService connected to {Base}", baseUrl);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Reconnect in 3s...");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_quotesHub is not null)
        {
            try { await _quotesHub.StopAsync(cancellationToken); } catch { }
            try { await _quotesHub.DisposeAsync(); } catch { }
        }
    }

    private static async Task TryMatchSymbolAsync(string symbol, decimal lastPrice, IServiceProvider services, CancellationToken ct)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var portfolio = services.GetRequiredService<IPortfolioService>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var candidates = await db.Orders
            .Where(o => o.Symbol == symbol
                        && o.Status == OrderStatus.Pending
                        && o.Type == OrderType.Limit
                        && o.LimitPrice != null
                        && ((o.Quantity > 0 && lastPrice <= o.LimitPrice)   // Buy crossing
                            || (o.Quantity < 0 && lastPrice >= o.LimitPrice))) // Sell crossing
            .OrderBy(o => o.SubmittedUtc)
            .Take(50)
            .ToListAsync(ct);

        if (candidates.Count == 0) return;

        foreach (var ord in candidates)
        {
            // Limit-or-better execution price
            var limit = ord.LimitPrice!.Value;
            var execPrice = ord.Quantity > 0
                ? Math.Min(lastPrice, limit)   // buy: never pay more than limit
                : Math.Max(lastPrice, limit);  // sell: never sell below limit

            string? reason = null;
            var ok = await portfolio.TryTradeAsync(ord.UserId, ord.Symbol, ord.Quantity, execPrice, ct, r => reason = r);

            if (!ok)
            {
                ord.Status = OrderStatus.Rejected;
                db.Add(new OutboxMessage
                {
                    Type = nameof(OrderRejectedEvent),
                    Payload = System.Text.Json.JsonSerializer.Serialize(
                        new OrderRejectedEvent(ord.OrderId, ord.UserId, ord.Symbol, ord.Quantity, reason ?? "Insufficient funds/quantity", DateTimeOffset.UtcNow))
                });
            }
            else
            {
                ord.Status = OrderStatus.Filled;
                ord.FillPrice = execPrice;
                ord.FilledUtc = DateTimeOffset.UtcNow;
                ord.Remaining = 0;

                db.Add(new OutboxMessage
                {
                    Type = nameof(OrderFilledEvent),
                    Payload = System.Text.Json.JsonSerializer.Serialize(
                        new OrderFilledEvent(ord.OrderId, ord.UserId, ord.Symbol, ord.Quantity, ord.FillPrice!.Value, ord.FilledUtc!.Value))
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
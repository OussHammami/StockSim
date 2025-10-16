using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StockSim.Application.Abstractions;
using StockSim.Application.Contracts.Orders;
using StockSim.Domain.Enums;
using StockSim.Domain.Models;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Persistence;
using StockSim.Infrastructure.Persistence.Entities;
using System.Text;
using System.Text.Json;

namespace StockSim.Web.Services;

public sealed class OrderConsumer(RabbitConnection rabitConnection, IServiceProvider serviceProvider) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = rabitConnection.Connection.CreateModel();
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
        {
            try
            {
                var cmd = JsonSerializer.Deserialize<OrderCommand>(Encoding.UTF8.GetString(eventArgs.Body.Span));
                if (cmd is null) { channel.BasicAck(eventArgs.DeliveryTag, false); return; }

                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var quotes = scope.ServiceProvider.GetRequiredService<LastQuotesCache>();
                var port = scope.ServiceProvider.GetRequiredService<IPortfolioService>();
                await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

                // idempotency
                if (await db.Set<ProcessedOrder>().FindAsync(new object[] { cmd.OrderId }, stoppingToken) is not null)
                { channel.BasicAck(eventArgs.DeliveryTag, false); return; }

                // ensure order row exists as Pending
                var ord = await db.Orders.SingleOrDefaultAsync(o => o.OrderId == cmd.OrderId, stoppingToken);
                if (ord is null)
                {
                    ord = new OrderEntity
                    {
                        OrderId = cmd.OrderId,
                        UserId = cmd.UserId,
                        Symbol = cmd.Symbol,
                        Quantity = cmd.Quantity,
                        Remaining = Math.Abs(cmd.Quantity),
                        Type = cmd.Type,
                        Tif = cmd.Tif,
                        LimitPrice = cmd.LimitPrice,
                        StopPrice = cmd.StopPrice,
                        Status = OrderStatus.Pending,
                        SubmittedUtc = DateTimeOffset.UtcNow
                    };
                    db.Orders.Add(ord);

                    // outbox: accepted
                    db.Add(new OutboxMessage
                    {
                        Type = nameof(OrderAcceptedEvent),
                        Payload = JsonSerializer.Serialize(
                            new OrderAcceptedEvent(ord.OrderId, ord.UserId, ord.Symbol, ord.Quantity, ord.SubmittedUtc))
                    });
                    await db.SaveChangesAsync(stoppingToken);
                }

                // Minimal matcher:
                // - Fill immediately for Market
                // - Fill immediately for crossing Limit (buy: last <= limit, sell: last >= limit)
                if (cmd.Type == OrderType.Market
                    || (cmd.Type == OrderType.Limit && cmd.LimitPrice.HasValue
                        && TryGetLast(quotes, cmd.Symbol, out var lastForLimit)
                        && IsCrossing(cmd.Quantity, lastForLimit.Price, cmd.LimitPrice.Value)))
                {
                    if (!TryGetLast(quotes, cmd.Symbol, out var last))
                    {
                        ord.Status = OrderStatus.Rejected;
                        db.Add(new OutboxMessage
                        {
                            Type = nameof(OrderRejectedEvent),
                            Payload = JsonSerializer.Serialize(
                                new OrderRejectedEvent(ord.OrderId, ord.UserId, ord.Symbol, ord.Quantity, "No quote", DateTimeOffset.UtcNow))
                        });
                    }
                    else
                    {
                        string? reason = null;
                        var ok = await port.TryTradeAsync(cmd.UserId, cmd.Symbol, cmd.Quantity, last.Price, stoppingToken, r => reason = r);
                        if (!ok)
                        {
                            ord.Status = OrderStatus.Rejected;
                            db.Add(new OutboxMessage
                            {
                                Type = nameof(OrderRejectedEvent),
                                Payload = JsonSerializer.Serialize(
                                    new OrderRejectedEvent(ord.OrderId, ord.UserId, ord.Symbol, ord.Quantity, reason ?? "Insufficient funds/quantity", DateTimeOffset.UtcNow))
                            });
                        }
                        else
                        {
                            ord.Status = OrderStatus.Filled;
                            ord.FillPrice = last.Price;
                            ord.FilledUtc = DateTimeOffset.UtcNow;
                            ord.Remaining = 0;

                            db.Add(new OutboxMessage
                            {
                                Type = nameof(OrderFilledEvent),
                                Payload = JsonSerializer.Serialize(
                                    new OrderFilledEvent(ord.OrderId, ord.UserId, ord.Symbol, ord.Quantity, ord.FillPrice.Value, ord.FilledUtc.Value))
                            });
                        }
                    }
                }
                // else: non-crossing Limit/Stop remain Pending for later matching

                db.Add(new ProcessedOrder { OrderId = cmd.OrderId });   // idempotency marker
                await db.SaveChangesAsync(stoppingToken);
                await tx.CommitAsync(stoppingToken);
                channel.BasicAck(eventArgs.DeliveryTag, false);
            }
            catch
            {
                channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };
        channel.BasicConsume(queue: rabitConnection.Options.Queue, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    static bool TryGetLast(LastQuotesCache cache, string symbol, out Quote q) => cache.TryGet(symbol, out q);
    static bool IsCrossing(int qty, decimal lastPrice, decimal limitPrice)
        => qty > 0 ? lastPrice <= limitPrice   // buy: at/below limit
                   : lastPrice >= limitPrice;  // sell: at/above limit
}

// simple in-memory last-quotes cache exposed via DI
public sealed class LastQuotesCache
{
    private readonly Dictionary<string, Quote> _d = new(StringComparer.OrdinalIgnoreCase);
    public void Upsert(Quote q) => _d[q.Symbol] = q;
    public bool TryGet(string s, out Quote q) => _d.TryGetValue(s, out q!);
}
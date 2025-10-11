using Microsoft.AspNetCore.SignalR;
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

public sealed class OrderConsumer(RabbitConnection rabitConnection, IServiceProvider serviceProvider, IHubContext<OrderHub> hub) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = rabitConnection.Connection.CreateModel();
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.Span);
                var command = JsonSerializer.Deserialize<OrderCommand>(json);
                if (command is null) { channel.BasicAck(eventArgs.DeliveryTag, false); return; }

                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);
                var portfolio = scope.ServiceProvider.GetRequiredService<IPortfolioService>();
                var quotesCache = scope.ServiceProvider.GetRequiredService<LastQuotesCache>();

                if (await db.Set<ProcessedOrder>().FindAsync(new object[] { command.OrderId }, stoppingToken) is not null)
                {
                    channel.BasicAck(eventArgs.DeliveryTag, false);
                    return;
                }


                if (!quotesCache.TryGet(command.Symbol, out var last))
                {
                    var rejOrd = await db.Orders.SingleAsync(o => o.OrderId == command.OrderId, stoppingToken);
                    rejOrd.Status = OrderStatus.Rejected;
                    await db.SaveChangesAsync(stoppingToken);

                    var ev = new OrderRejectedEvent(rejOrd.OrderId, rejOrd.UserId, rejOrd.Symbol, rejOrd.Quantity, "No quote", DateTimeOffset.UtcNow);
                    db.Add(new OutboxMessage
                    {
                        Type = nameof(OrderRejectedEvent),
                        Payload = JsonSerializer.Serialize(ev)
                    });

                    db.Add(new ProcessedOrder { OrderId = command.OrderId });
                    await db.SaveChangesAsync(stoppingToken);
                    await tx.CommitAsync(stoppingToken);
                    channel.BasicAck(eventArgs.DeliveryTag, false);
                    return;
                }

                string? reason = null;
                var ok = await portfolio.TryTradeAsync(command.UserId, command.Symbol, command.Quantity, last.Price, stoppingToken, r => reason = r);
                var ord = await db.Orders.SingleAsync(o => o.OrderId == command.OrderId, stoppingToken);

                if (!ok)
                {
                    ord.Status = OrderStatus.Rejected;
                    ord.FillPrice = null;
                    ord.FilledUtc = null;

                    var ev = new OrderRejectedEvent(ord.OrderId, ord.UserId, ord.Symbol, ord.Quantity,
                                                    reason ?? "Insufficient funds/quantity", DateTimeOffset.UtcNow);
                    db.Add(new OutboxMessage { Type = nameof(OrderRejectedEvent), Payload = JsonSerializer.Serialize(ev) });

                    db.Add(new ProcessedOrder { OrderId = command.OrderId });
                    await db.SaveChangesAsync(stoppingToken);
                    await tx.CommitAsync(stoppingToken);
                    channel.BasicAck(eventArgs.DeliveryTag, false);
                    return;
                }

                ord.Status = OrderStatus.Filled;
                ord.FillPrice = last.Price;
                ord.FilledUtc = DateTimeOffset.UtcNow;

                var filled = new OrderFilledEvent(ord.OrderId, ord.UserId, ord.Symbol, ord.Quantity, ord.FillPrice!.Value, ord.FilledUtc!.Value);
                db.Add(new OutboxMessage
                {
                    Type = nameof(OrderFilledEvent),
                    Payload = JsonSerializer.Serialize(filled)
                }); 
                
                db.Add(new ProcessedOrder { OrderId = command.OrderId });
                await db.SaveChangesAsync(stoppingToken);
                await tx.CommitAsync(stoppingToken);

                channel.BasicAck(eventArgs.DeliveryTag, false);
            }
            catch
            {
                channel.BasicNack(eventArgs.DeliveryTag, false, requeue: false);
            }
        };
        channel.BasicConsume(queue: rabitConnection.Options.Queue, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }
}

// simple in-memory last-quotes cache exposed via DI
public sealed class LastQuotesCache
{
    private readonly Dictionary<string, Quote> _d = new(StringComparer.OrdinalIgnoreCase);
    public void Upsert(Quote q) => _d[q.Symbol] = q;
    public bool TryGet(string s, out Quote q) => _d.TryGetValue(s, out q!);
}

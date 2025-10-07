using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StockSim.Shared.Models;
using StockSim.Web.Data;
using StockSim.Web.Data.Trading;
using StockSim.Web.Hubs;
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
                var portfolio = scope.ServiceProvider.GetRequiredService<IPortfolioService>();
                var quotesCache = scope.ServiceProvider.GetRequiredService<LastQuotesCache>();

                // resolve scoped services per message

                if (!quotesCache.TryGet(command.Symbol, out var last))
                {
                    // skip unknown symbol; ack to avoid poison loop
                    var rej = db.Orders.Single(o => o.OrderId == command.OrderId);
                    rej.Status = OrderStatus.Rejected;
                    db.SaveChanges();
                    await hub.Clients.Group($"u:{command.UserId}")
                       .SendAsync("order", new { command.OrderId, Status = OrderStatus.Rejected.ToString() });
                    channel.BasicAck(eventArgs.DeliveryTag, false); return;
                }

                await portfolio.TryTradeAsync(command.UserId, command.Symbol, command.Quantity, last.Price, stoppingToken);

                var ord = db.Orders.Single(o => o.OrderId == command.OrderId);
                ord.Status = OrderStatus.Filled;
                ord.FillPrice = last.Price;
                ord.FilledUtc = DateTimeOffset.UtcNow;
                db.SaveChanges();

                await hub.Clients.Group($"u:{command.UserId}")
                   .SendAsync("order", new
                   {
                       command.OrderId,
                       ord.Symbol,
                       ord.Quantity,
                       ord.FillPrice,
                       Status = ord.Status.ToString(),
                       ord.FilledUtc
                   });

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

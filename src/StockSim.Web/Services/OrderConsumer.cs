using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StockSim.Shared.Models;

namespace StockSim.Web.Services;

public sealed class OrderConsumer(RabbitConnection rc, IServiceProvider sp) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ch = rc.Connection.CreateModel();
        var consumer = new AsyncEventingBasicConsumer(ch);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                var cmd = JsonSerializer.Deserialize<OrderCommand>(json);
                if (cmd is null) { ch.BasicAck(ea.DeliveryTag, false); return; }

                // resolve scoped services per message
                using var scope = sp.CreateScope();
                var portfolio = scope.ServiceProvider.GetRequiredService<IPortfolioServiceAsync>();
                var quotesCache = scope.ServiceProvider.GetRequiredService<LastQuotesCache>();

                if (!quotesCache.TryGet(cmd.Symbol, out var last))
                {
                    // skip unknown symbol; ack to avoid poison loop
                    ch.BasicAck(ea.DeliveryTag, false); return;
                }

                await portfolio.TryTradeAsync(cmd.Symbol, cmd.Quantity, last.Price, stoppingToken);
                ch.BasicAck(ea.DeliveryTag, false);
            }
            catch
            {
                ch.BasicNack(ea.DeliveryTag, false, requeue: false);
            }
        };
        ch.BasicConsume(queue: rc.Options.Queue, autoAck: false, consumer: consumer);
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

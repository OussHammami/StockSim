using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Persistence.Trading;
using System.Text;

public sealed class TradingOutboxPublisher : BackgroundService
{
    private readonly TradingDbContext _db;
    private readonly ILogger<TradingOutboxPublisher> _log;
    private readonly RabbitConnection _rabbit;

    public TradingOutboxPublisher(TradingDbContext db, ILogger<TradingOutboxPublisher> log, RabbitConnection rabbit)
    {
        _db = db;
        _log = log;
        _rabbit = rabbit;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        _log.LogInformation("TradingOutboxDispatcher started");
        using var channel = _rabbit.CreateChannel();
        var queue = _rabbit.Options.Queue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var batch = await _db.Outbox
                .Where(x => x.SentAt == null && x.Attempts < 10)
                .OrderBy(x => x.CreatedAt)
                .Take(100)
                .ToListAsync(stoppingToken);

                if (batch.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                foreach (var m in batch)
                {
                    try
                    {
                        var body = Encoding.UTF8.GetBytes(m.Data);
                        var props = channel.CreateBasicProperties();
                        props.Persistent = _rabbit.Options.Durable;
                        props.Type = m.Type;
                        props.MessageId = m.Id.ToString();
                        props.Timestamp = new AmqpTimestamp(m.OccurredAt.ToUnixTimeSeconds());
                        if (!string.IsNullOrWhiteSpace(m.DedupeKey))
                            props.CorrelationId = m.DedupeKey;

                        // Default exchange -> queue.
                        channel.BasicPublish(exchange: "",
                                             routingKey: queue,
                                             basicProperties: props,
                                             body: body);

                        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                        m.SentAt = DateTimeOffset.UtcNow;
                        m.Attempts += 1;
                        _log.LogInformation("Trading outbox published id={Id} type={Type} subject={Subject}", m.Id, m.Type, m.Subject);
                    }
                    catch (Exception ex)
                    {
                        m.Attempts += 1;
                        _log.LogError(ex, "Trading publish failed id={Id} type={Type} attempt={Attempts}", m.Id, m.Type, m.Attempts);
                    }
                }

                await _db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Trading outbox loop error");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            _log.LogInformation("TradingOutboxDispatcher stopped");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Persistence.Portfolioing;
using System.Text;

public sealed class PortfolioOutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PortfolioOutboxPublisher> _log;
    private readonly RabbitConnection _rabbit;

    public PortfolioOutboxPublisher(
        ILogger<PortfolioOutboxPublisher> log,
        RabbitConnection rabbit,
        IServiceScopeFactory scopeFactory)
    {
        _log = log;
        _rabbit = rabbit;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("PortfolioOutboxDispatcher started");
        using var channel = _rabbit.CreateChannel();
        var queue = _rabbit.Options.Queue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();

                var batch = await db.Outbox
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

                        channel.BasicPublish(exchange: "",
                                             routingKey: queue,
                                             basicProperties: props,
                                             body: body);

                        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                        m.SentAt = DateTimeOffset.UtcNow;
                        m.Attempts += 1;

                        _log.LogInformation("Portfolio outbox published id={Id} type={Type} subject={Subject}", m.Id, m.Type, m.Subject);
                    }
                    catch (Exception ex)
                    {
                        m.Attempts += 1;
                        _log.LogError(ex, "Portfolio publish failed id={Id} type={Type} attempt={Attempts}", m.Id, m.Type, m.Attempts);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Portfolio outbox loop error");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _log.LogInformation("PortfolioOutboxDispatcher stopped");
    }
}

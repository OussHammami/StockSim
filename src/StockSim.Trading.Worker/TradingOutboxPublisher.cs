using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Persistence.Portfolioing;
using StockSim.Infrastructure.Persistence.Trading;
using StockSim.Application.Telemetry;
using System.Diagnostics;
using System.Text;

public sealed class TradingOutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TradingOutboxPublisher> _log;
    private readonly RabbitConnection _rabbit;

    public TradingOutboxPublisher(IServiceScopeFactory scopeFactory, ILogger<TradingOutboxPublisher> log, RabbitConnection rabbit)
    {
        _scopeFactory = scopeFactory;
        _log = log;
        _rabbit = rabbit;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        _log.LogInformation(
            "TradingOutboxDispatcher started. OTelSourceName={SourceName} HasListeners={HasListeners}",
            Telemetry.OrdersSource.Name,
            Telemetry.OrdersSource.HasListeners());
        using var channel = _rabbit.CreateChannel();
        var queue = _rabbit.Options.Queue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
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
                        using var activity = Telemetry.OrdersSource.StartActivity("rabbitmq.publish", ActivityKind.Producer);
                        if (activity is null)
                        {
                            _log.LogWarning(
                                "OTel activity not created for rabbitmq.publish. SourceName={SourceName} HasListeners={HasListeners}",
                                Telemetry.OrdersSource.Name,
                                Telemetry.OrdersSource.HasListeners());
                        }
                        activity?.SetTag("messaging.system", "rabbitmq");
                        activity?.SetTag("messaging.destination", queue);
                        activity?.SetTag("messaging.operation", "publish");
                        activity?.SetTag("messaging.message_id", m.Id.ToString());
                        activity?.SetTag("messaging.message_type", m.Type);

                        var body = Encoding.UTF8.GetBytes(m.Data);
                        var props = channel.CreateBasicProperties();
                        props.Persistent = _rabbit.Options.Durable;
                        props.Type = m.Type;
                        props.MessageId = m.Id.ToString();
                        props.Timestamp = new AmqpTimestamp(m.OccurredAt.ToUnixTimeSeconds());
                        if (!string.IsNullOrWhiteSpace(m.DedupeKey))
                            props.CorrelationId = m.DedupeKey;

                        props.Headers ??= new Dictionary<string, object>();
                        Propagators.DefaultTextMapPropagator.Inject(
                            new PropagationContext(Activity.Current?.Context ?? default, OpenTelemetry.Baggage.Current),
                            props.Headers,
                            static (carrier, key, value) => carrier[key] = Encoding.UTF8.GetBytes(value));

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

                await db.SaveChangesAsync(stoppingToken);
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

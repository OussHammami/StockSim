using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StockSim.Application.Abstractions.Inbox;
using StockSim.Infrastructure.Messaging;
using System.Text;

namespace StockSim.Portfolio.Worker;

/// <summary>
/// RabbitMQ consumer that reads Trading events, ensures idempotency via EfInboxStore,
/// and dispatches to ITradingEventHandler.
/// </summary>
public sealed class TradingEventConsumer : BackgroundService
{
    private readonly RabbitConnection _rabbit;
    private readonly IInboxStore<ITradingInboxContext> _inbox;
    private readonly ITradingEventHandler _handler;
    private readonly ILogger<TradingEventConsumer> _log;

    private IModel? _channel;

    public TradingEventConsumer(
        RabbitConnection rabbit,
        IInboxStore<ITradingInboxContext> inbox,
        ITradingEventHandler handler,
        ILogger<TradingEventConsumer> log)
    {
        _rabbit = rabbit;
        _inbox = inbox;
        _handler = handler;
        _log = log;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run the consumer loop on a dedicated Task.
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private async Task ConsumeLoop(CancellationToken ct)
    {
        var queue = _rabbit.Options.Queue;
        _log.LogInformation("TradingEventConsumer starting. Queue={Queue}", queue);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                _channel?.Dispose();
                _channel = _rabbit.CreateChannel();
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 32, global: false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (_, ea) =>
                {
                    // Extract headers
                    var headers = ExtractHeaders(ea.BasicProperties);
                    var messageId = ea.BasicProperties?.MessageId ?? Guid.NewGuid().ToString();
                    var dedupeKey = ea.BasicProperties?.CorrelationId ?? messageId; // prefer CorrelationId if present
                    var type = ea.BasicProperties?.Type ?? "(unknown)";
                    var bodyJson = Encoding.UTF8.GetString(ea.Body.ToArray());

                    // Idempotency check
                    if (await _inbox.SeenAsync(dedupeKey, ct))
                    {
                        _log.LogDebug("Duplicate message ignored. key={Key} type={Type}", dedupeKey, type);
                        _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                        return;
                    }

                    try
                    {
                        await _handler.HandleAsync(type, bodyJson, headers, ct);
                        await _inbox.MarkAsync(dedupeKey, ct);
                        _channel!.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        // Do not ack. Let shutdown close channel and requeue in-flight deliveries.
                    }
                    catch (Exception ex)
                    {
                        // Basic policy: first redelivery allowed, then dead-letter by rejecting.
                        var redelivered = ea.Redelivered;
                        _log.LogError(ex, "Handler failed. type={Type} key={Key} redelivered={Redelivered}", type, dedupeKey, redelivered);

                        if (redelivered)
                        {
                            // Nack without requeue to avoid poison loops. Use DLX if configured on the queue.
                            _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                        }
                        else
                        {
                            // Let it retry once.
                            _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                        }
                    }
                };

                var consumerTag = _channel.BasicConsume(
                    queue: queue,
                    autoAck: false,
                    consumer: consumer);

                _log.LogInformation("Consuming with tag={Tag}", consumerTag);

                // Wait here until cancelled. Reconnects handled by outer loop on exception.
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Consumer loop error. Reconnecting shortly.");
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
            finally
            {
                try { _channel?.Close(); } catch { /* ignore */ }
                try { _channel?.Dispose(); } catch { /* ignore */ }
                _channel = null;
            }
        }

        _log.LogInformation("TradingEventConsumer stopped.");
    }

    private static IReadOnlyDictionary<string, string?> ExtractHeaders(IBasicProperties? props)
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["messageId"] = props?.MessageId,
            ["correlationId"] = props?.CorrelationId,
            ["type"] = props?.Type,
            ["appId"] = props?.AppId
        };

        // Also flatten ApplicationHeaders if present.
        if (props?.Headers != null)
        {
            foreach (var kv in props.Headers)
            {
                dict[kv.Key] = kv.Value switch
                {
                    byte[] bytes => Encoding.UTF8.GetString(bytes),
                    ReadOnlyMemory<byte> mem => Encoding.UTF8.GetString(mem.ToArray()),
                    string s => s,
                    sbyte sb => sb.ToString(),
                    byte b => b.ToString(),
                    short sh => sh.ToString(),
                    ushort ush => ush.ToString(),
                    int i => i.ToString(),
                    uint ui => ui.ToString(),
                    long l => l.ToString(),
                    ulong ul => ul.ToString(),
                    _ => kv.Value?.ToString()
                };
            }
        }

        return dict;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try { _channel?.Close(); } catch { /* ignore */ }
        try { _channel?.Dispose(); } catch { /* ignore */ }
        _channel = null;
        return base.StopAsync(cancellationToken);
    }
}

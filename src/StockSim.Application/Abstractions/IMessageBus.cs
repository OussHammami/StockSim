namespace StockSim.Application.Abstractions;

public interface IMessageBus
{
    Task PublishAsync<T>(string topicOrQueue, T message, CancellationToken ct = default);
}

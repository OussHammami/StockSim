using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using StockSim.Shared.Models;

namespace StockSim.Web.Services;

public interface IOrderPublisher
{
    void Publish(OrderCommand cmd);
}

public sealed class OrderPublisher(RabbitConnection rc) : IOrderPublisher
{
    public void Publish(OrderCommand cmd)
    {
        using var ch = rc.Connection.CreateModel();
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cmd));
        ch.BasicPublish(exchange: "", routingKey: rc.Options.Queue, basicProperties: null, body: body);
    }
}

using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using StockSim.Application.Contracts.Orders;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;
using StockSim.Application.Abstractions;

namespace StockSim.Infrastructure.Messaging;

public sealed class OrderPublisher(RabbitConnection rc, IServiceScopeFactory scopeFactory) : IOrderPublisher
{
    static readonly ActivitySource Orders = new("StockSim.Orders");
    public void Publish(OrderCommand cmd)
    {   
        using var act = Orders.StartActivity("orders.publish", ActivityKind.Producer);
        act?.SetTag("order.id", cmd.OrderId);
        act?.SetTag("symbol", cmd.Symbol);
        act?.SetTag("qty", cmd.Quantity);

        using var channel = rc.Connection.CreateModel();
        var props = channel.CreateBasicProperties();
        props.Headers ??= new Dictionary<string, object>();
        Propagators.DefaultTextMapPropagator.Inject(
            new PropagationContext(Activity.Current?.Context ?? default, Baggage.Current),
            props.Headers,
            (d, k, v) => d[k] = Encoding.UTF8.GetBytes(v));
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cmd));
        channel.BasicPublish(exchange: "", routingKey: rc.Options.Queue, basicProperties: null, body: body);
    }
}

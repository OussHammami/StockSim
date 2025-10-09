using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using StockSim.Application.Contracts.Orders;
using StockSim.Domain.Enums;
using StockSim.Infrastructure.Persistence;
using StockSim.Infrastructure.Persistence.Entities;
using System.Text;
using System.Text.Json;

namespace StockSim.Infrastructure.Messaging;

public interface IOrderPublisher
{
    void Publish(OrderCommand cmd);
}

public sealed class OrderPublisher(RabbitConnection rc, IServiceScopeFactory scopeFactory) : IOrderPublisher
{
    public void Publish(OrderCommand cmd)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Orders.Add(new OrderEntity
            {
                OrderId = cmd.OrderId,
                UserId = cmd.UserId,
                Symbol = cmd.Symbol,
                Quantity = cmd.Quantity,
                SubmittedUtc = DateTimeOffset.UtcNow,
                Status = OrderStatus.Pending
            });
            db.SaveChanges();
        }

        using var channel = rc.Connection.CreateModel();
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cmd));
        channel.BasicPublish(exchange: "", routingKey: rc.Options.Queue, basicProperties: null, body: body);
    }
}

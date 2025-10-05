using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace StockSim.Web.Services;

public sealed class RabbitOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Pass { get; set; } = "guest";
    public string Queue { get; set; } = "stocksim.orders";
}

public sealed class RabbitConnection : IDisposable
{
    public IConnection Connection { get; }
    public RabbitOptions Options { get; }
    public RabbitConnection(IOptions<RabbitOptions> opt)
    {
        Options = opt.Value;
        var factory = new ConnectionFactory
        {
            HostName = Options.Host,
            Port = Options.Port,
            UserName = Options.User,
            Password = Options.Pass,
            DispatchConsumersAsync = true
        };
        Connection = factory.CreateConnection("stocksim-web");
        using var ch = Connection.CreateModel();
        ch.QueueDeclare(Options.Queue, durable: false, exclusive: false, autoDelete: false);
    }
    public void Dispose() => Connection.Dispose();
}

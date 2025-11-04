using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace StockSim.Infrastructure.Messaging;

public sealed class RabbitOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Pass { get; set; } = "guest";
    public string Queue { get; set; } = "stocksim.orders";
    public bool Durable { get; set; } = true;
}

public sealed class RabbitConnection : IDisposable
{
    public IConnection Connection { get; }
    public RabbitOptions Options { get; }

    public RabbitConnection(IOptions<RabbitOptions> opt)
    {
        Options = opt.Value ?? throw new ArgumentNullException(nameof(opt));

        var factory = new ConnectionFactory
        {
            HostName = Options.Host,
            Port = Options.Port,
            UserName = Options.User,
            Password = Options.Pass,
            DispatchConsumersAsync = true
        };

        Connection = factory.CreateConnection("stocksim-workers");

        // Ensure target queue exists.
        using var ch = CreateChannel();
        ch.QueueDeclare(
            queue: Options.Queue,
            durable: Options.Durable,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public IModel CreateChannel()
    {
        var ch = Connection.CreateModel();
        ch.ConfirmSelect(); // publisher confirms
        return ch;
    }

    public void Dispose()
    {
        try
        {
            if (Connection.IsOpen) Connection.Close();
        }
        finally
        {
            Connection.Dispose();
        }
    }
}

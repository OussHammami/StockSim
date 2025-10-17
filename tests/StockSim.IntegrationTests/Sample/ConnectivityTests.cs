using Npgsql;
using RabbitMQ.Client;
using StockSim.IntegrationTests.Containers;
using Xunit;

namespace StockSim.IntegrationTests.Sample;

[Collection(nameof(ContainersCollection))]
public sealed class ConnectivityTests(ContainersFixture fx)
{
    private readonly ContainersFixture _fx = fx;

    [Fact]
    public async Task Postgres_accepts_connections()
    {
        await using var conn = new NpgsqlConnection(_fx.PgConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT 1", conn);
        var result = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        Assert.Equal(1, result);
    }

    [Fact]
    public void RabbitMQ_accepts_connections()
    {
        var factory = new ConnectionFactory { Uri = new Uri(_fx.RabbitConnectionString) };
        using var conn = factory.CreateConnection();
        using var ch = conn.CreateModel();
        ch.QueueDeclare(queue: "it-hello", durable: false, exclusive: false, autoDelete: true);
    }
}
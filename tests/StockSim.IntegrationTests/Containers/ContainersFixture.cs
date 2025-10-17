using System;
using System.Threading.Tasks;
using Npgsql;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace StockSim.IntegrationTests.Containers;

public sealed class ContainersFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; private set; } = default!;
    public RabbitMqContainer Rabbit { get; private set; } = default!;

    public string PgConnectionString { get; private set; } = default!;
    public string RabbitConnectionString { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        // Config
        var pgUser = "stocksim";
        var pgPass = "stocksim";
        var pgDb   = "stocksim_test";

        // Testcontainers v3 typed modules
        Postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithUsername(pgUser)
            .WithPassword(pgPass)
            .WithDatabase(pgDb)
            .Build();

        // RabbitMQ default creds are guest/guest. Set explicitly for clarity.
        const string rabbitUser = "guest";
        const string rabbitPass = "guest";

        Rabbit = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithUsername(rabbitUser)
            .WithPassword(rabbitPass)
            .Build();

        await Postgres.StartAsync();
        await Rabbit.StartAsync();

        // Connection strings
        PgConnectionString = Postgres.GetConnectionString();

        // Build AMQP URI from mapped port and known credentials
        var rabbitHost = Rabbit.Hostname;
        var rabbitPort = Rabbit.GetMappedPublicPort(5672);
        RabbitConnectionString =
            $"amqp://{Uri.EscapeDataString(rabbitUser)}:{Uri.EscapeDataString(rabbitPass)}@{rabbitHost}:{rabbitPort}/";
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            Postgres.DisposeAsync().AsTask(),
            Rabbit.DisposeAsync().AsTask()
        );
    }
}
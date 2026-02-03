using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using StockSim.Application;
using StockSim.Application.Abstractions.Events;
using StockSim.Application.Integration;
using StockSim.Application.Orders;
using StockSim.Application.Orders.Handlers;
using StockSim.Application.Portfolios;
using StockSim.Domain.Orders.Events;
using Microsoft.Extensions.Logging;
using StockSim.Application.Abstractions.Outbox;

namespace StockSim.Web.IntegrationTests.TestHost;

public class TestingWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // 1) Kill background services that might dial Rabbit or other infra
            services.RemoveAll<IHostedService>();

            // 2) Remove any bus/outbox infra if present
            services.RemoveAll(typeof(RabbitMQ.Client.IConnectionFactory));   // harmless if not present
            services.RemoveAll(typeof(RabbitMQ.Client.IConnection));
            services.RemoveAll(typeof(RabbitMQ.Client.IModel));

            // 3) Your app services and fakes (as you already had)
            services.RemoveAll<IOrderRepository>();
            services.RemoveAll<IPortfolioRepository>();
            services.RemoveAll<IOutboxWriter<IPortfolioOutboxContext>>();
            services.RemoveAll<IOutboxWriter<ITradingOutboxContext>>();
            services.AddApplicationCore();
            services.AddSingleton<IOrderRepository, Fakes.InMemoryOrderRepository>();
            services.AddSingleton<IPortfolioRepository, Fakes.InMemoryPortfolioRepository>();
            services.AddSingleton<IOutboxWriter<IPortfolioOutboxContext>, Fakes.InMemoryOutboxWriter>();
            services.AddSingleton<IOutboxWriter<ITradingOutboxContext>, Fakes.InMemoryOutboxWriter>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<
                IDomainEventHandler<OrderAccepted>,
                OrderAcceptedHandler>());

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
        builder.ConfigureLogging(lb =>
        {
            lb.ClearProviders();
            lb.AddConsole();
            lb.AddDebug();
        });
        // Optional: override config to disable messaging/health checks in Testing
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:Enabled"] = "false",
                ["HealthChecks:RabbitEnabled"] = "false",

                // Satisfy ValidateOnStart for RabbitOptions in test host
                ["Rabbit:Host"] = "localhost",
                ["Rabbit:Port"] = "5672",
                ["Rabbit:User"] = "test",
                ["Rabbit:Pass"] = "test",
                ["Rabbit:Queue"] = "test",
                ["Rabbit:Durable"] = "false"
            });
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:Enabled"] = "false",
                ["HealthChecks:RabbitEnabled"] = "false"
            });
        });
    }
}

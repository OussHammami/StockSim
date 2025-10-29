﻿using Microsoft.AspNetCore.Authentication;
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
            services.RemoveAll<IOutboxWriter>();
            services.AddApplicationCore();
            services.AddSingleton<IOrderRepository, Fakes.InMemoryOrderRepository>();
            services.AddSingleton<IPortfolioRepository, Fakes.InMemoryPortfolioRepository>();
            services.AddSingleton<IOutboxWriter, Fakes.InMemoryOutboxWriter>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<
                IDomainEventHandler<OrderAccepted>,
                OrderAcceptedHandler>());

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });

        // Optional: override config to disable messaging/health checks in Testing
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:Enabled"] = "false",
                ["HealthChecks:RabbitEnabled"] = "false"
            });
        });
    }
}

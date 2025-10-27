using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using StockSim.Infrastructure.Persistence;
using StockSim.Web;

namespace StockSim.IntegrationTests.Security;

public class TestingWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Swap DB with InMemory
            var toRemoveDb = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext))
                .ToList();
            foreach (var d in toRemoveDb) services.Remove(d);
            services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase("stocksim-tests"));

            // Remove RabbitMQ-driven hosted services from the Web app
            var badHosted = services
                .Where(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType != null &&
                    d.ImplementationType.Namespace != null &&
                    d.ImplementationType.Namespace.StartsWith("StockSim.Web") &&
                    (
                        d.ImplementationType.Name.Contains("Consumer") ||
                        d.ImplementationType.Name.Contains("Dispatcher") ||
                        d.ImplementationType.Name.Contains("Matcher") ||
                        d.ImplementationType.Name.Contains("Worker") ||
                        d.ImplementationType.Name.Contains("Background")
                    ))
                .ToList();

            foreach (var d in badHosted) services.Remove(d);

            // Add a single no-op hosted service so host lifecycle remains healthy
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, NoOpHostedService>());
        });
    }
}
file sealed class NoOpHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

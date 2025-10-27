using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            // Remove the real ApplicationDbContext registration
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext))
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            // Register EF Core InMemory
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("stocksim-tests"));
        });
    }
}

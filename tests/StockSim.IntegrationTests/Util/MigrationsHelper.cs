using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockSim.Infrastructure.Persistence;

namespace StockSim.IntegrationTests.Util;

public static class MigrationsHelper
{
    public static async Task<ApplicationDbContext> ApplyAsync(string connectionString, CancellationToken ct = default)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(connectionString));

        var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(ct);
        return db;
    }
}
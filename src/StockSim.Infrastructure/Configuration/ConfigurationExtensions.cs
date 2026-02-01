using System;
using Microsoft.Extensions.Configuration;

namespace StockSim.Infrastructure.Configuration;

public static class ConfigurationExtensions
{
    public static string GetRequiredConnectionString(this IConfiguration cfg, string name)
    {
        var cs = cfg.GetConnectionString(name);

        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException($"Missing required connection string: ConnectionStrings:{name}");

        return cs;
    }
}

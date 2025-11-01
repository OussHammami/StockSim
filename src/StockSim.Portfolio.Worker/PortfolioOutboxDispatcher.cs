using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockSim.Infrastructure.Persistence.Portfolioing;

public sealed class PortfolioOutboxDispatcher : BackgroundService
{
    private readonly PortfolioDbContext _db;
    private readonly ILogger<PortfolioOutboxDispatcher> _log;

    public PortfolioOutboxDispatcher(PortfolioDbContext db, ILogger<PortfolioOutboxDispatcher> log)
    {
        _db = db;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var batch = await _db.Outbox
                .Where(x => x.SentAt == null && x.Source == "portfolio")
                .OrderBy(x => x.CreatedAt)
                .Take(100)
                .ToListAsync(stoppingToken);

            if (batch.Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            foreach (var m in batch)
            {
                try
                {
                    // TODO: publish to bus
                    _log.LogInformation("Publish {Type} {Subject}", m.Type, m.Subject);
                    m.SentAt = DateTimeOffset.UtcNow;
                    m.Attempts++;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Failed to publish {Id}", m.Id);
                    m.Attempts++;
                }
            }

            await _db.SaveChangesAsync(stoppingToken);
        }
    }
}

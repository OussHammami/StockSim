using StockSim.Application.Integration;
using StockSim.Application.Telemetry;
using StockSim.Infrastructure.Persistence.Portfolioing;
using StockSim.Infrastructure.Persistence.Trading;

namespace StockSim.Infrastructure.Outbox;

public sealed class EfOutboxWriter : IOutboxWriter
{
    private readonly TradingDbContext _trading;
    private readonly PortfolioDbContext _portfolio;

    public EfOutboxWriter(TradingDbContext trading, PortfolioDbContext portfolio)
    {
        _trading = trading;
        _portfolio = portfolio;
    }

    public async Task WriteAsync(IEnumerable<IntegrationEvent> events, CancellationToken ct = default)
    {
        int count = 0;
        foreach (var e in events)
        {
            var msg = new OutboxMessage
            {
                Type = e.Type,
                Source = e.Source,
                Subject = e.Subject,
                OccurredAt = e.OccurredAt,
                Data = e.Data,
                SchemaVersion = e.SchemaVersion,
                DedupeKey = e.DedupeKey
            };

            if (e.Source == "trading")
                await _trading.Outbox.AddAsync(msg, ct);
            else if (e.Source == "portfolio")
                await _portfolio.Outbox.AddAsync(msg, ct);
            else
                throw new InvalidOperationException($"Unknown source '{e.Source}'.");
            count++;
        }

        // let each context save only if it has pending entries
        if (_trading.ChangeTracker.HasChanges())
            await _trading.SaveChangesAsync(ct);
        if (_portfolio.ChangeTracker.HasChanges())
            await _portfolio.SaveChangesAsync(ct);
        if (count > 0) Telemetry.OutboxEventsWritten.Add(count);
    }
}

using Microsoft.EntityFrameworkCore;
using StockSim.Application.Abstractions.Inbox;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Domain.Orders;
using StockSim.Infrastructure.Inbox;
using StockSim.Infrastructure.Outbox;

namespace StockSim.Infrastructure.Persistence.Trading;

public class TradingDbContext: DbContext, ITradingOutboxContext, ITradingInboxContext
{
    public const string Schema = "trading";

    public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema(Schema);
        b.ApplyConfigurationsFromAssembly(typeof(TradingDbContext).Assembly);
    }
}

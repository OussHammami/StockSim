using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockSim.Infrastructure.Outbox;

namespace StockSim.Infrastructure.Persistence.Trading.Configurations;

internal sealed class OutboxConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("outbox_messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasMaxLength(200).IsRequired();
        b.Property(x => x.Source).HasMaxLength(50).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        b.Property(x => x.Data).IsRequired();
        b.HasIndex(x => new { x.SentAt, x.CreatedAt });
        b.HasIndex(x => x.DedupeKey);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockSim.Infrastructure.Inbox;

namespace StockSim.Infrastructure.Persistence.Portfolioing.Configurations;

internal sealed class InboxConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> b)
    {
        b.ToTable("inbox_messages");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.DedupeKey).IsUnique();
        b.Property(x => x.DedupeKey).HasMaxLength(300).IsRequired();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;

namespace StockSim.Infrastructure.Persistence.Trading.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("orders");

        // Key: OrderId <-> Guid
        b.HasKey(x => x.Id);

        b.Property(x => x.Id)
            .HasConversion(
                id => id.Value,            // OrderId -> Guid
                g => OrderId.From(g))     // Guid -> OrderId
            .ValueGeneratedNever()
            .HasColumnName("id");

        // User
        b.Property(x => x.UserId)
            .HasColumnName("user_id");

        // Symbol VO
        b.OwnsOne(x => x.Symbol, s =>
        {
            s.Property(p => p.Value)
             .HasColumnName("symbol")
             .HasMaxLength(15)
             .IsRequired();
        });

        // Side, Type, State as ints (or use .HasConversion<string>() if you prefer)
        b.Property(x => x.Side)
            .HasColumnName("side")
            .HasConversion<int>();

        b.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<int>();

        b.Property(x => x.State)
            .HasColumnName("state")
            .HasConversion<int>();

        // Quantity VO
        b.OwnsOne(x => x.Quantity, q =>
        {
            q.Property(p => p.Value)
             .HasColumnName("quantity")
             .HasColumnType("numeric(18,4)")
             .IsRequired();
        });

        // LimitPrice Price? nullable
        b.OwnsOne(x => x.LimitPrice, lp =>
        {
            lp.Property(p => p.Value)
              .HasColumnName("limit_price")
              .HasColumnType("numeric(18,2)");
        });

        // FilledQuantity and AverageFillPrice as primitives on aggregate
        b.Property(x => x.FilledQuantity)
            .HasColumnName("filled_quantity")
            .HasColumnType("numeric(18,4)");

        b.Property(x => x.AverageFillPrice)
            .HasColumnName("avg_fill_price")
            .HasColumnType("numeric(18,2)");

        // Timestamps if present (keep if your entity has them)
        // b.Property(x => x.CreatedAt).HasColumnName("created_at");
        // b.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        // Domain events are not persisted
        b.Ignore(x => x.DomainEvents);
    }
}

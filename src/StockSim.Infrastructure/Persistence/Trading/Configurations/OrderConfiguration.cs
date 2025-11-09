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
                id => id.Value,            
                g => OrderId.From(g))     
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

       
        b.Property(x => x.Side)
            .HasColumnName("side")
            .HasConversion<int>();

        b.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<int>();

        b.Property(x => x.State)
            .HasColumnName("state")
            .HasConversion<int>();
                
        b.OwnsOne(x => x.Quantity, q =>
        {
            q.Property(p => p.Value)
             .HasColumnName("quantity")
             .HasColumnType("numeric(18,4)")
             .IsRequired();
        });
                
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

        b.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        // Domain events are not persisted
        b.Ignore(x => x.DomainEvents);

        b.Property(x => x.TimeInForce)
         .HasColumnName("time_in_force")
         .HasConversion<int>();

        b.Property(x => x.ExpiresAt)
         .HasColumnName("expires_at");

        b.HasIndex(x => new { x.State, x.CreatedAt });
        b.HasIndex(x => new { x.Symbol, x.State });
    }
}

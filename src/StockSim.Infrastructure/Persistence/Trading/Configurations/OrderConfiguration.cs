using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;

namespace StockSim.Infrastructure.Persistence.Trading.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        var orderIdConv = new ValueConverter<OrderId, Guid>(v => v.Value, v => OrderId.From(v));
        var symbolConv  = new ValueConverter<Symbol, string>(v => v.Value, v => Symbol.From(v));
        var qtyConv     = new ValueConverter<Quantity, decimal>(v => v.Value, v => Quantity.From(v));
        var priceConv   = new ValueConverter<Price?, decimal?>(v => v == null ? null : v.Value, v => v == null ? null : Price.From(v.Value));

        b.ToTable("orders");
        b.HasKey("_id");

        // Map Aggregate Id as alternate key and index
        b.Property(o => o.Id).HasConversion(orderIdConv).HasColumnName("order_id").IsRequired();
        b.HasIndex(o => o.Id).IsUnique();

        b.Property<Guid>("_id").HasColumnName("id").ValueGeneratedOnAdd(); // DB primary key
        b.HasKey("id");

        b.Property(o => o.UserId).HasColumnName("user_id").IsRequired();
        b.Property(o => o.Symbol).HasConversion(symbolConv).HasColumnName("symbol").HasMaxLength(15).IsRequired();
        b.Property(o => o.Side).HasColumnName("side").IsRequired();
        b.Property(o => o.Type).HasColumnName("type").IsRequired();
        b.Property(o => o.Quantity).HasConversion(qtyConv).HasColumnName("qty").HasColumnType("numeric(18,4)").IsRequired();
        b.Property(o => o.LimitPrice).HasConversion(priceConv).HasColumnName("limit_price").HasColumnType("numeric(18,4)");
        b.Property(o => o.State).HasColumnName("state").IsRequired();

        b.Property(o => o.FilledQuantity).HasColumnName("filled_qty").HasColumnType("numeric(18,4)").IsRequired();
        b.Property(o => o.AverageFillPrice).HasColumnName("avg_fill_price").HasColumnType("numeric(18,4)").IsRequired();

        // Ignore domain events list
        b.Ignore(o => o.DomainEvents);
        b.Ignore(o => o.RemainingQuantity);
    }
}

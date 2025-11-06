using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockSim.Domain.Portfolio;
using StockSim.Domain.ValueObjects;

namespace StockSim.Infrastructure.Persistence.Portfolioing.Configurations;

internal sealed class PortfolioConfiguration : IEntityTypeConfiguration<Portfolio>
{
    public void Configure(EntityTypeBuilder<Portfolio> b)
    {
        b.ToTable("portfolios");

        // Key
        b.HasKey(x => x.Id);

        // PortfolioId <-> Guid
        b.Property(x => x.Id)
            .HasConversion(
                id => id.Value,                  // PortfolioId -> Guid
                g  => PortfolioId.From(g))       // Guid -> PortfolioId
            .ValueGeneratedNever()
            .HasColumnName("id");

        // User
        b.Property(x => x.UserId)
            .HasColumnName("user_id");

        // Money owned values
        b.OwnsOne(x => x.Cash, m =>
        {
            m.Property(p => p.Amount)
             .HasColumnName("cash")
             .HasColumnType("numeric(18,2)");
        });

        b.OwnsOne(x => x.ReservedCash, m =>
        {
            m.Property(p => p.Amount)
             .HasColumnName("reserved_cash")
             .HasColumnType("numeric(18,2)");
        });

        // Positions collection
        b.Navigation(x => x.Positions).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.OwnsMany(x => x.Positions, pos =>
        {
            pos.ToTable("positions");
            pos.WithOwner().HasForeignKey("portfolio_id");

            pos.Property<int>("id");
            pos.HasKey("id");

            pos.OwnsOne(p => p.Symbol, s =>
            {
                s.Property(v => v.Value)
                 .HasColumnName("symbol")
                 .HasMaxLength(15)
                 .IsRequired();
            });

            pos.Property(p => p.Quantity)
               .HasColumnName("quantity");

            pos.Property(p => p.AvgCost)
               .HasColumnName("avg_cost")
               .HasColumnType("numeric(18,2)");
        });

        // domain events are not persisted
        b.Ignore(x => x.DomainEvents);
    }
}

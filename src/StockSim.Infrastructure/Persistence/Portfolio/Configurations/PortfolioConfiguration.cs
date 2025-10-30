using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StockSim.Domain.ValueObjects;

namespace StockSim.Infrastructure.Persistence.Portfolioing.Configurations;

internal sealed class PortfolioConfiguration : IEntityTypeConfiguration<Domain.Portfolio.Portfolio>
{
    public void Configure(EntityTypeBuilder<Domain.Portfolio.Portfolio> b)
    {
        var portfolioIdConv = new ValueConverter<PortfolioId, Guid>(v => v.Value, v => PortfolioId.From(v));
        var moneyConv = new ValueConverter<Money, decimal>(v => v.Amount, v => Money.From(v));
        var symbolConv = new ValueConverter<Symbol, string>(v => v.Value, v => Symbol.From(v));

        b.ToTable("portfolios");
        b.HasKey("_id");

        b.Property<Guid>("_id").HasColumnName("id").ValueGeneratedOnAdd();
        b.Property(p => p.Id).HasConversion(portfolioIdConv).HasColumnName("portfolio_id").IsRequired();
        b.HasIndex(p => p.Id).IsUnique();

        b.Property(p => p.UserId).HasColumnName("user_id").IsRequired();
        b.Property(p => p.Cash).HasConversion(moneyConv).HasColumnName("cash").HasColumnType("numeric(18,2)").IsRequired();
        b.Property(p => p.ReservedCash).HasConversion(moneyConv).HasColumnName("reserved_cash").HasColumnType("numeric(18,2)").IsRequired();

        // Positions as owned collection
        b.OwnsMany(p => p.Positions, nb =>
        {
            nb.ToTable("positions");
            nb.WithOwner().HasForeignKey("portfolio_fk");
            nb.Property<Guid>("id");
            nb.HasKey("id");
            nb.Property(x => x.Symbol).HasConversion(symbolConv).HasColumnName("symbol").HasMaxLength(15).IsRequired();
            nb.Property(x => x.Quantity).HasColumnName("qty").HasColumnType("numeric(18,4)").IsRequired();
            nb.Property(x => x.AvgCost).HasColumnName("avg_cost").HasColumnType("numeric(18,4)").IsRequired();
            nb.HasIndex("portfolio_fk", "symbol").IsUnique();
        });

        // Reserved shares dictionary persisted as separate table
        b.Ignore(p => p.DomainEvents);
    }
}

using System.ComponentModel.DataAnnotations;

namespace StockSim.Infrastructure.Persistence.Entities;

public sealed class PortfolioEntity
{
    [Key] public string UserId { get; set; } = default!;
    public decimal Cash { get; set; }
}

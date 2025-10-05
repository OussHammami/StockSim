using System.ComponentModel.DataAnnotations;

namespace StockSim.Web.Data.Trading;

public sealed class PortfolioEntity
{
    [Key] public string UserId { get; set; } = default!;
    public decimal Cash { get; set; }
}

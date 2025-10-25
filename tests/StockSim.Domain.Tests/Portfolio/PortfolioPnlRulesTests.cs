using StockSim.Domain.Entities;
using StockSim.Domain.Models;

namespace StockSim.Domain.Tests.Portfolio;

public sealed class PortfolioPnlRulesTests
{
    private static (decimal mv, decimal upnl) Compute(IReadOnlyList<Position> positions, IReadOnlyDictionary<string, Quote> quotes)
    {
        decimal mv = 0, upnl = 0;
        foreach (var p in positions)
        {
            if (!quotes.TryGetValue(p.Symbol, out var q)) continue;
            mv += p.Quantity * q.Price;
            upnl += p.Quantity * (q.Price - p.AvgPrice);
        }
        return (mv, upnl);
    }

    [Fact]
    public void Given_PositionsAndQuotes_When_Priced_Then_MarketValueAndUPnLAreSumAcrossSymbols()
    {
        var positions = new List<Position>
        {
            new() { Symbol = "AAPL", Quantity = 10, AvgPrice = 100m },
            new() { Symbol = "MSFT", Quantity = -5, AvgPrice = 300m }, // short
            new() { Symbol = "NVDA", Quantity = 0, AvgPrice = 0m }
        };

        var quotes = new Dictionary<string, Quote>
        {
            ["AAPL"] = new("AAPL", 110m, 0, DateTimeOffset.UtcNow),
            ["MSFT"] = new("MSFT", 280m, 0, DateTimeOffset.UtcNow),
            ["NVDA"] = new("NVDA", 450m, 0, DateTimeOffset.UtcNow),
        };

        var (mv, upnl) = Compute(positions, quotes);

        // MV = 10*110 + (-5)*280 = 1100 - 1400 = -300
        // UPnL = 10*(110-100) + (-5)*(280-300) = 100 + 100 = 200
        Assert.Equal(-300m, mv);
        Assert.Equal(200m, upnl);
    }

    [Fact]
    public void Given_MissingQuote_When_Priced_Then_IgnoreThatSymbol()
    {
        var positions = new List<Position> { new() { Symbol = "AAPL", Quantity = 10, AvgPrice = 100m } };
        var quotes = new Dictionary<string, Quote>(); // no AAPL

        var (mv, upnl) = Compute(positions, quotes);

        Assert.Equal(0m, mv);
        Assert.Equal(0m, upnl);
    }
}

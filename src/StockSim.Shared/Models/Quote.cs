namespace StockSim.Shared.Models;

public record Quote(string Symbol, decimal Price, decimal Change, DateTimeOffset TimeUtc);

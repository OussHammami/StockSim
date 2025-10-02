using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using StockSim.Shared.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// simple price store
var prices = new ConcurrentDictionary<string, Quote>();
var symbols = new[] { "AAPL", "MSFT", "AMZN", "GOOGL", "NVDA", "TSLA", "META" };

// background price jitter
builder.Services.AddHostedService(sp => new PriceWorker(prices, symbols));

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

// list quotes
app.MapGet("/api/quotes", ([FromQuery] string? symbolsCsv) =>
{
    IEnumerable<string> requested = symbols;
    if (!string.IsNullOrWhiteSpace(symbolsCsv))
        requested = symbolsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    return requested
        .Select(s => prices.TryGetValue(s, out var q) ? q : new Quote(s, 0m, 0m, DateTimeOffset.UtcNow))
        .ToArray();
});

app.Run();

file sealed class PriceWorker : BackgroundService
{
    private readonly ConcurrentDictionary<string, Quote> _prices;
    private readonly string[] _symbols;
    private readonly Random _rng = new();

    public PriceWorker(ConcurrentDictionary<string, Quote> prices, string[] symbols)
    { _prices = prices; _symbols = symbols; }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        // seed
        foreach (var s in _symbols)
            _prices[s] = new Quote(s, 100m + (decimal)_rng.NextDouble() * 100m, 0m, DateTimeOffset.UtcNow);

        while (!token.IsCancellationRequested)
        {
            foreach (var s in _symbols)
            {
                var old = _prices[s];
                var pct = ((decimal)_rng.NextDouble() - 0.5m) * 0.02m; // ±1%
                var newPrice = Math.Max(1m, old.Price * (1m + pct));
                var change = newPrice - old.Price;
                _prices[s] = new Quote(s, decimal.Round(newPrice, 2), decimal.Round(change, 2), DateTimeOffset.UtcNow);
            }
            await Task.Delay(5000, token);
        }
    }
}

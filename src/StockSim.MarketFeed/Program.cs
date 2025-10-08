using Microsoft.AspNetCore.Mvc;
using StockSim.MarketFeed;
using StockSim.Domain.Models;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// CORS: allow the Blazor Web origin (update the port to your Web app HTTPS port)
const string AllowWeb = "_allowWeb";
builder.Services.AddCors(o => o.AddPolicy(AllowWeb, p =>
    p.WithOrigins("https://localhost:7197")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// shared state via DI
builder.Services.AddSingleton(new ConcurrentDictionary<string, Quote>());
builder.Services.AddSingleton(new[] { "AAPL", "MSFT", "AMZN", "GOOGL", "NVDA", "TSLA", "META" });
builder.Services.AddHostedService<PriceWorker>();


var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseCors(AllowWeb);
// list quotes
app.MapGet("/api/quotes", (ConcurrentDictionary<string, Quote> prices, string? symbolsCsv) =>
{
    var all = prices.Keys.ToArray();
    IEnumerable<string> req = string.IsNullOrWhiteSpace(symbolsCsv)
        ? all
        : symbolsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    return req.Select(s => prices.TryGetValue(s, out var q)
        ? q : new Quote(s, 0m, 0m, DateTimeOffset.UtcNow));
}).RequireCors(AllowWeb);

app.MapHub<QuoteHub>("/hubs/quotes").RequireCors(AllowWeb); ;
app.Run();

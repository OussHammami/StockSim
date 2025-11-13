using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace StockSim.Portfolio.Worker.External.Trading;

public sealed class HttpMarketPriceProvider : IMarketPriceProvider
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public HttpMarketPriceProvider(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _baseUrl = cfg.GetValue<string>("MarketFeed:BaseUrl")?.TrimEnd('/') ?? "http://marketfeed:8081";
    }

    public async Task<decimal?> GetAskAsync(string symbol, CancellationToken ct = default)
    {
        // MarketFeed exposes GET /api/quotes?symbolsCsv=SYM and returns an array
        var url = $"{_baseUrl}/api/quotes?symbolsCsv={Uri.EscapeDataString(symbol)}";

        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;

        var items = await resp.Content.ReadFromJsonAsync<List<FeedQuoteDto>>(cancellationToken: ct);
        var q = items?.FirstOrDefault();
        return q?.Ask;
    }

    private sealed class FeedQuoteDto
    {
        public string Symbol { get; set; } = "";
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public decimal Last { get; set; }
        public DateTimeOffset Ts { get; set; }
    }
}
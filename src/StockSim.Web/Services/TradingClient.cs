using StockSim.Domain.Orders;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace StockSim.Web.Services;

public sealed class TradingClient
{
    private readonly HttpClient _http;
    public TradingClient(HttpClient http) => _http = http;

    public sealed record PlaceOrderDto(Guid? UserId, string Symbol, OrderSide Side, OrderType Type, decimal Quantity, decimal? LimitPrice);
    public sealed record CancelOrderDto(Guid? UserId, string? Reason);

    public async Task<string> PlaceAsync(PlaceOrderDto dto, CancellationToken ct = default)
    {
        using var res = await _http.PostAsJsonAsync("/api/trading/orders", dto, ct);
        if (res.IsSuccessStatusCode)
        {
            // API returns Created with body = id string
            var id = await res.Content.ReadAsStringAsync(ct);
            return id.Trim('"'); // tolerate JSON string or raw
        }

        // Bubble up ProblemDetails when available
        var content = await res.Content.ReadAsStringAsync(ct);
        try
        {
            var problem = JsonSerializer.Deserialize<ProblemDetailsPayload>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (problem is not null)
                throw new TradingClientException(problem.Title ?? "Request failed", res.StatusCode, problem.Detail, problem.Extensions);
        }
        catch
        {
            // ignore JSON parse errors
        }
        res.EnsureSuccessStatusCode();
        throw new TradingClientException("Request failed", res.StatusCode, content, null);
    }

    public async Task CancelAsync(string orderId, CancelOrderDto dto, CancellationToken ct = default)
    {
        using var res = await _http.PostAsJsonAsync($"/api/trading/orders/{orderId}/cancel", dto, ct);
        if (!res.IsSuccessStatusCode)
        {
            var content = await res.Content.ReadAsStringAsync(ct);
            throw new TradingClientException("Cancel failed", res.StatusCode, content, null);
        }
    }

    private sealed record ProblemDetailsPayload(string? Title, string? Detail, Dictionary<string, object>? Extensions);
    public sealed class TradingClientException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string? Detail { get; }
        public Dictionary<string, object>? Extensions { get; }
        public TradingClientException(string message, HttpStatusCode status, string? detail, Dictionary<string, object>? ext)
            : base(message) { StatusCode = status; Detail = detail; Extensions = ext; }
    }
}

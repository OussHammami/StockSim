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
    public sealed record OrderDto(
        string Id,
        string Symbol,
        string Side,
        string Type,
        decimal Quantity,
        string State,
        decimal FilledQuantity,
        decimal RemainingQuantity,
        decimal AverageFillPrice,
        decimal? LimitPrice,
        DateTime CreatedAt
    );

    public async Task<string> PlaceAsync(PlaceOrderDto dto, CancellationToken ct = default)
    {
        using var res = await _http.PostAsJsonAsync("/api/trading/orders", dto, ct);
        if (res.IsSuccessStatusCode)
        {
            var id = await res.Content.ReadAsStringAsync(ct);
            return id.Trim('"'); 
        }

        var content = await res.Content.ReadAsStringAsync(ct);
        TryThrowProblem(res.StatusCode, content, "Request failed");
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

    public async Task<IReadOnlyList<OrderDto>> GetRecentAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        using var res = await _http.GetAsync($"/api/trading/orders?skip={skip}&take={take}", ct);
        res.EnsureSuccessStatusCode();
        var list = await res.Content.ReadFromJsonAsync<List<OrderDto>>(cancellationToken: ct);
        return list ?? new List<OrderDto>();
    }

    private void TryThrowProblem(HttpStatusCode status, string content, string defaultMessage)
    {
        try
        {
            var problem = JsonSerializer.Deserialize<ProblemDetailsPayload>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (problem is not null)
                throw new TradingClientException(problem.Title ?? defaultMessage, status, problem.Detail, problem.Extensions);
        }
        catch { /* ignore */ }
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

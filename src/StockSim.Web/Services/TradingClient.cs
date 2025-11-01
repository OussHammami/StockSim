using System.Net.Http.Json;
using StockSim.Domain.Orders;

namespace StockSim.Web.Services;

public sealed class TradingClient
{
    private readonly HttpClient _http;
    public TradingClient(HttpClient http) => _http = http;

    public sealed record PlaceOrderDto(Guid? UserId, string Symbol, OrderSide Side, OrderType Type, decimal Quantity, decimal? LimitPrice);
    public sealed record CancelOrderDto(Guid? UserId, string? Reason);

    public async Task<string> PlaceAsync(PlaceOrderDto dto, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync("/api/trading/orders", dto, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);
    }

    public async Task CancelAsync(string orderId, CancelOrderDto dto, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync($"/api/trading/orders/{orderId}/cancel", dto, ct);
        res.EnsureSuccessStatusCode();
    }
}

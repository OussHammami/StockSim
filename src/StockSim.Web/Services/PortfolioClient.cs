using System.Net.Http.Json;

namespace StockSim.Web.Services;

public sealed class PortfolioClient
{
    private readonly HttpClient _http;
    public PortfolioClient(HttpClient http) => _http = http;

    public sealed record SummaryDto(string PortfolioId, Guid UserId, decimal Cash, decimal ReservedCash, IEnumerable<PositionDto> Positions);
    public sealed record PositionDto(string Symbol, decimal Quantity, decimal AvgCost);
    public sealed record DepositDto(decimal Amount, Guid? UserId);

    public async Task<SummaryDto?> GetSummaryAsync(CancellationToken ct = default)
    {
        var res = await _http.GetAsync("/api/portfolio/summary", ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<SummaryDto>(cancellationToken: ct);
    }

    public async Task DepositAsync(decimal amount, Guid? userId, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync("/api/portfolio/deposit", new DepositDto(amount, userId), ct);
        res.EnsureSuccessStatusCode();
    }
}

namespace StockSim.Web.Hubs;
public sealed record OrderHubMessage(
    Guid OrderId, string UserId, string Symbol, int Quantity,
    string Status, decimal? FillPrice, string? Reason, DateTimeOffset TimeUtc);

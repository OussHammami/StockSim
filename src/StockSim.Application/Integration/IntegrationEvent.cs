using System.Text.Json;

namespace StockSim.Application.Integration;

public sealed record IntegrationEvent(
    string Id,
    string Type,           // e.g. "trading.order.accepted"
    string Source,         // e.g. "trading"
    string Subject,        // e.g. order id or portfolio id
    DateTimeOffset OccurredAt,
    string Data,           // JSON payload
    string SchemaVersion = "1",
    string? DedupeKey = null)
{
    public static IntegrationEvent Create<T>(string type, string source, string subject, T data, DateTimeOffset occurredAt, string? dedupeKey = null)
    {
        var id = Guid.NewGuid().ToString("N");
        var json = JsonSerializer.Serialize(data);
        return new IntegrationEvent(id, type, source, subject, occurredAt, json, "1", dedupeKey);
    }
}

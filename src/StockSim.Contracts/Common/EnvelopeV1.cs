namespace StockSim.Contracts.Common;

/// <summary>Integration event envelope. Transport-agnostic.</summary>
public sealed record EnvelopeV1(
    string Id,
    string Type,            // e.g. "trading.order.accepted"
    string Source,          // e.g. "trading"
    string Subject,         // e.g. order id or portfolio id
    DateTimeOffset OccurredAt,
    string SchemaVersion = "1",
    string? DedupeKey = null);

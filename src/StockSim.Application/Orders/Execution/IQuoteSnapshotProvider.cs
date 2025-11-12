namespace StockSim.Application.Orders.Execution;

public interface IQuoteSnapshotProvider
{
    QuoteSnapshot? Get(string symbol);
}

public sealed record QuoteSnapshot(
    string Symbol,
    decimal Bid,
    decimal Ask,
    decimal? Last,
    DateTimeOffset Timestamp);
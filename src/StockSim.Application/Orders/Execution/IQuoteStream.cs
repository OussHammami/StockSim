namespace StockSim.Application.Orders.Execution;

public interface IQuoteStream
{
    IDisposable Subscribe(Func<QuoteMsg, Task> onQuote);
}

// Transport payload coming from the quotes hub (or any quote source)
public sealed record QuoteMsg(
    string Symbol,
    decimal Bid,
    decimal Ask,
    decimal? Last,
    DateTimeOffset Ts);
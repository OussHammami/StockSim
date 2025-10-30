using StockSim.Domain.ValueObjects;

namespace StockSim.Application.MarketData.Feed;

public static class QuoteAcl
{
    public static Quote? Map(FeedQuoteDto dto)
    {
        if (dto is null) return null;
        if (string.IsNullOrWhiteSpace(dto.Ticker)) return null;
        if (dto.Bid is null || dto.Ask is null) return null;
        if (dto.Bid < 0m || dto.Ask < 0m) return null;
        if (dto.TsUtc is null) return null;
        if (dto.Ask < dto.Bid) return null;

        var symbol = Symbol.From(dto.Ticker);
        var bid = Price.From(dto.Bid.Value);
        var ask = Price.From(dto.Ask.Value);
        var last = dto.Last is null ? null : Price.From(dto.Last.Value);
        return Quote.From(symbol, bid, ask, last, dto.TsUtc.Value);
    }
}

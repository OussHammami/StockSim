using StockSim.Application.MarketData.Feed;

namespace StockSim.Application.Tests.MarketData;

public class QuoteAclTests
{
    public static TheoryData<string?, decimal?, decimal?> InvalidCases => new()
    {
        { null,    100m, 101m },
        { "AAPL",  null, 101m },
        { "AAPL", 100m,  null },
        { "AAPL",  -1m, 101m },
        { "AAPL", 101m, 100m }, // ask < bid handled by Quote ctor
    };

    [Fact]
    public void Valid_Maps_To_Quote()
    {
        var dto = new FeedQuoteDto("aapl", 100m, 100.5m, 100.2m, DateTimeOffset.UtcNow);
        var q = QuoteAcl.Map(dto);
        Assert.NotNull(q);
        Assert.Equal("AAPL", q!.Symbol.Value);
        Assert.Equal(100m, q.Bid.Value);
        Assert.Equal(100.5m, q.Ask.Value);
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void Invalid_Returns_Null(string? t, decimal? bid, decimal? ask)
    {
        var dto = new FeedQuoteDto(t ?? "", bid, ask, 100m, DateTimeOffset.UtcNow);
        var q = QuoteAcl.Map(dto);
        Assert.Null(q);
    }
}

using StockSim.Application.MarketData.Feed;

namespace StockSim.Application.Tests.MarketData;

public class QuoteAclTests
{
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
    [InlineData(null, 100, 101)]
    [InlineData("AAPL", null, 101)]
    [InlineData("AAPL", 100, null)]
    [InlineData("AAPL", -1, 101)]
    [InlineData("AAPL", 101, 100)] // ask < bid handled by Quote
    public void Invalid_Returns_Null(string? t, decimal? bid, decimal? ask)
    {
        var dto = new FeedQuoteDto(t ?? "", bid, ask, 100, DateTimeOffset.UtcNow);
        var q = QuoteAcl.Map(dto);
        Assert.Null(q);
    }
}

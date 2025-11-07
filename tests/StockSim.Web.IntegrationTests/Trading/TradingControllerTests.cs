using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
using StockSim.Application.Portfolios;
using StockSim.Domain.ValueObjects;
using StockSim.Web.IntegrationTests.Fakes;
using StockSim.Web.IntegrationTests.TestHost;
using System.Net.Http.Json;

namespace StockSim.Web.IntegrationTests.Trading;
public class TradingControllerTests : IClassFixture<TestingWebAppFactory>
{
    private readonly TestingWebAppFactory _factory;
    public TradingControllerTests(TestingWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Place_Limit_Buy_Returns_Created_And_Writes_Outbox()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("http://localhost"), });
        client.DefaultRequestHeaders.Authorization = new("Test");

        // seed portfolio with cash
        var repo = _factory.Services.GetRequiredService<IPortfolioRepository>();
        var mem = (InMemoryPortfolioRepository)repo;
        var uid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff");
        var p = new Domain.Portfolio.Portfolio(PortfolioId.New(), uid);
        p.Deposit(Money.From(1000m));
        mem.Seed(p);

        var dto = new
        {
            UserId = (Guid?)uid,
            Symbol = "AAPL",
            Side = Domain.Orders.OrderSide.Buy,
            Type = Domain.Orders.OrderType.Limit,
            Quantity = 5m,
            LimitPrice = 100m
        };

        var res = await client.PostAsJsonAsync("/api/trading/orders", dto);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

        var outbox = _factory.Services.GetRequiredService<IOutboxWriter<ITradingOutboxContext>>();
        var memOutbox = (InMemoryOutboxWriter) outbox;
        memOutbox.Items.Should().Contain(x => x.Type == "trading.order.accepted");
    }
}

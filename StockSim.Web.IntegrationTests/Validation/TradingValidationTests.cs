using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StockSim.Application.Portfolios;
using StockSim.Domain.ValueObjects;
using StockSim.Web.IntegrationTests.Fakes;
using StockSim.Web.IntegrationTests.TestHost;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace StockSim.Web.IntegrationTests.Validation;

public class TradingValidationTests : IClassFixture<TestingWebAppFactory>
{
    private readonly TestingWebAppFactory _factory;
    private static readonly Guid U = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeffffffff");

    public TradingValidationTests(TestingWebAppFactory factory) => _factory = factory;

    private HttpClient Client()
    {
        var c = _factory.CreateClient(new() { BaseAddress = new Uri("http://localhost") });
        c.DefaultRequestHeaders.Authorization = new("Test");
        return c;
    }

    [Fact]
    public async Task Empty_Symbol_Returns_400_ProblemDetails()
    {
        var client = Client();
        var dto = new
        {
            UserId = (Guid?)U,
            Symbol = "",
            Side = StockSim.Domain.Orders.OrderSide.Buy,
            Type = StockSim.Domain.Orders.OrderType.Market,
            Quantity = 1m,
            LimitPrice = (decimal?)null
        };

        var res = await client.PostAsJsonAsync("/api/trading/orders", dto);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Prefer validation problem details for 400 from model validation
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();

        // basic ProblemDetails fields
        json.GetProperty("status").GetInt32().Should().Be(400);

        // do NOT pin 'type'; just ensure it exists and is non-empty
        json.TryGetProperty("type", out var type).Should().BeTrue();
        type.GetString().Should().NotBeNullOrWhiteSpace();

        // validation payload shape: has "errors" with a key for Symbol
        json.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.TryGetProperty("Symbol", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Market_Order_With_LimitPrice_Returns_400()
    {
        var client = Client();
        var dto = new
        {
            UserId = (Guid?)U,
            Symbol = "AAPL",
            Side = Domain.Orders.OrderSide.Buy,
            Type = Domain.Orders.OrderType.Market,
            Quantity = 1m,
            LimitPrice = 10m // invalid for market
        };

        var res = await client.PostAsJsonAsync("/api/trading/orders", dto);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Limit_Order_Missing_LimitPrice_Returns_400()
    {
        var client = Client();
        var dto = new
        {
            UserId = (Guid?)U,
            Symbol = "AAPL",
            Side = Domain.Orders.OrderSide.Buy,
            Type = Domain.Orders.OrderType.Limit,
            Quantity = 1m,
            LimitPrice = (decimal?)null // invalid for limit
        };

        var res = await client.PostAsJsonAsync("/api/trading/orders", dto);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Valid_Limit_Order_Returns_201()
    {
        // seed cash so validation passes end-to-end
        var repo = _factory.Services.GetRequiredService<IPortfolioRepository>();
        var mem = (InMemoryPortfolioRepository) repo;
        var p = new Domain.Portfolio.Portfolio(PortfolioId.New(), U);
        p.Deposit(Money.From(1000m));
        mem.Seed(p);

        var client = Client();
        var dto = new
        {
            UserId = (Guid?)U,
            Symbol = "AAPL",
            Side = Domain.Orders.OrderSide.Buy,
            Type = Domain.Orders.OrderType.Limit,
            Quantity = 2m,
            LimitPrice = 100m
        };

        var res = await client.PostAsJsonAsync("/api/trading/orders", dto);
        res.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private sealed record ProblemDetailsLike(string? Type, string? Title, int? Status, string? Detail, string? Instance);
}

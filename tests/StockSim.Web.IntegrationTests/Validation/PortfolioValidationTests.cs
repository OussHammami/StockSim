using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using StockSim.Web.IntegrationTests.TestHost;
using Xunit;

namespace StockSim.Web.IntegrationTests.Validation;

public class PortfolioValidationTests : IClassFixture<TestingWebAppFactory>
{
    private readonly TestingWebAppFactory _factory;
    public PortfolioValidationTests(TestingWebAppFactory factory) => _factory = factory;

    private HttpClient Client()
    {
        var c = _factory.CreateClient(new() { BaseAddress = new Uri("http://localhost") });
        c.DefaultRequestHeaders.Authorization = new("Test");
        return c;
    }

    [Fact]
    public async Task Cancel_Order_Reason_Too_Long_Returns_400()
    {
        var client = Client();
        var reason = new string('x', 400);

        var res = await client.PostAsJsonAsync("/api/trading/orders/00000000-0000-0000-0000-000000000001/cancel",
            new { UserId = (Guid?)null, Reason = reason });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // basic ProblemDetails check
        var pd = await res.Content.ReadFromJsonAsync<ProblemDetailsLike>();
        pd.Should().NotBeNull();
        pd!.Status.Should().Be((int)HttpStatusCode.BadRequest);
    }

    private sealed record ProblemDetailsLike(string? Type, string? Title, int? Status, string? Detail, string? Instance);
}

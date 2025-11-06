using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using StockSim.Web.IntegrationTests.TestHost;
using System.Net.Http.Json;

namespace StockSim.Web.IntegrationTests.Portfolio;
public class PortfolioControllerTests : IClassFixture<TestingWebAppFactory>
{
    private readonly TestingWebAppFactory _factory;
    public PortfolioControllerTests(TestingWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Summary_Then_Deposit_Works()
    {
        var client = _factory.CreateClient(new() { BaseAddress = new Uri("http://localhost") });
        client.DefaultRequestHeaders.Authorization = new("Test");

        var s1 = await client.GetAsync("/api/portfolio/summary");
        var body = await s1.Content.ReadAsStringAsync();
        s1.EnsureSuccessStatusCode();

        var dep = new { Amount = 123.45m, UserId = (Guid?)null };
        var res = await client.PostAsJsonAsync("/api/portfolio/deposit", dep);
        res.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var doc = await client.GetFromJsonAsync<System.Text.Json.JsonDocument>("/api/portfolio/summary");
        var cash = doc!.RootElement.GetProperty("cash").GetDecimal();
        cash.Should().BeGreaterThanOrEqualTo(123.45m);
    }
}

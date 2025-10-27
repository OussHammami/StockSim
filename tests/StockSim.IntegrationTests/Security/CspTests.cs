using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
namespace StockSim.IntegrationTests.Security;
public class CspTests : IClassFixture<TestingWebAppFactory>
{
    private readonly WebApplicationFactory<StockSim.Web.Program> _factory;

    public CspTests(TestingWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Root_Sends_CSP_Header()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var res = await client.GetAsync("/test/csp");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True(res.Headers.Contains("Content-Security-Policy"));
        var csp = string.Join(" ", res.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("connect-src", csp);
        Assert.Contains("'self'", csp);
        Assert.Contains("https://apis.example.com", csp);
    }
}

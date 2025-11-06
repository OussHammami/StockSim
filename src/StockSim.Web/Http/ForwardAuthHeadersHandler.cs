using System.Net.Http.Headers;

namespace StockSim.Web.Http;

public sealed class ForwardAuthHeadersHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _http;

    public ForwardAuthHeadersHandler(IHttpContextAccessor http) => _http = http;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = _http.HttpContext;
        if (ctx is not null)
        {
            // BaseAddress to same host if request is relative
            if (!request.RequestUri!.IsAbsoluteUri)
            {
                var baseUri = new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host}");
                request.RequestUri = new Uri(baseUri, request.RequestUri);
            }

            // Forward Cookie header for cookie auth
            if (ctx.Request.Headers.TryGetValue("Cookie", out var cookie) && cookie.Count > 0)
                request.Headers.TryAddWithoutValidation("Cookie", (IEnumerable<string>)cookie);

            // Forward bearer if present
            if (ctx.Request.Headers.TryGetValue("Authorization", out var auth) && auth.Count > 0)
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(auth!);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

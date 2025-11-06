using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace StockSim.Web.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetStableUserId(this ClaimsPrincipal user)
    {
        var raw =
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            user.FindFirst("sub")?.Value ??
            user.FindFirst("uid")?.Value ??
            user.Identity?.Name ??
            throw new InvalidOperationException("No user identifier.");

        if (Guid.TryParse(raw, out var id)) return id;

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        Span<byte> g = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(g);
        return new Guid(g);
    }
}

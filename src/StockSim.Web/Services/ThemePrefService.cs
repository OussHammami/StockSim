using Microsoft.AspNetCore.Http;

public interface IThemePrefService
{
    bool Get(HttpContext ctx);
    void Set(HttpContext ctx, bool isDark);
}
public sealed class ThemePrefService : IThemePrefService
{
    private const string CookieName = "stocksim_theme";
    public bool Get(HttpContext ctx)
        => string.Equals(ctx.Request.Cookies[CookieName], "dark", StringComparison.OrdinalIgnoreCase);
    public void Set(HttpContext ctx, bool isDark)
    {
        ctx.Response.Cookies.Append(
            CookieName,
            isDark ? "dark" : "light",
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, HttpOnly = false, SameSite = SameSiteMode.Lax, Secure = true });
    }
}

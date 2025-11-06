using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;

namespace StockSim.Web.Setup;

public static class SecurityExtensions
{
    public static IServiceCollection AddSecurity(this IServiceCollection services, IWebHostEnvironment env)
    {
        services.ConfigureApplicationCookie(o =>
        {
            o.Cookie.Name = ".stocksim.auth";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = env.IsDevelopment() ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
            o.SlidingExpiration = true;
            o.ExpireTimeSpan = TimeSpan.FromHours(8);
            o.LoginPath = "/Account/Login";
            o.AccessDeniedPath = "/Account/AccessDenied";
        });

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddFixedWindowLimiter("global", opt =>
            {
                opt.PermitLimit = 300;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });
        });

        return services;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, string[] origins)
    {
        return app.Use(async (ctx, next) =>
        {
            var env = ctx.RequestServices.GetRequiredService<IHostEnvironment>();

            var connect = new List<string> { "'self'" };
            foreach (var o in origins)
            {
                connect.Add(o);
                if (o.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    connect.Add("wss://" + o["https://".Length..]);
                if (o.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    connect.Add("ws://" + o["http://".Length..]);
            }

            if (env.IsDevelopment())
            {
                connect.AddRange(new[]
                {
                    "http://localhost:*",
                    "ws://localhost:*",
                    "wss://localhost:*"
                });
            }

            var csp =
                $"default-src 'self'; " +
                $"base-uri 'self'; " +
                $"frame-ancestors 'none'; " +
                $"img-src 'self' data:; " +
                $"font-src 'self' data:; " +
                $"style-src 'self' 'unsafe-inline'; " +
                $"script-src 'self' 'unsafe-inline'; " +
                $"connect-src {string.Join(' ', connect)}";

            ctx.Response.Headers["Content-Security-Policy"] = csp;
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
            ctx.Response.Headers["X-Frame-Options"] = "DENY";
            ctx.Response.Headers["X-TraceId"] = Activity.Current?.Id ?? ctx.TraceIdentifier;

            await next();
        });
    }
}

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Diagnostics;

namespace StockSim.Web.Setup;

public static class PipelineExtensions
{
    public static WebApplication UseBasePipeline(this WebApplication app)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseSerilogRequestLogging(opts =>
        {
            opts.EnrichDiagnosticContext = (dc, http) =>
            {
                dc.Set("UserId", http.User?.Identity?.Name);
                dc.Set("TraceId", Activity.Current?.Id ?? http.TraceIdentifier);
                dc.Set("Path", http.Request.Path);
            };
        });
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.MapGet("/healthz", () => Results.Ok("ok")).AllowAnonymous();
        app.MapGet("/readyz", () => Results.Ok("ready")).AllowAnonymous();

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async ctx =>
            {
                var feat = ctx.Features.Get<IExceptionHandlerFeature>();
                var ex = feat?.Error;

                var logger = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Unhandled");
                var traceId = Activity.Current?.Id ?? ctx.TraceIdentifier;

                logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", traceId);

                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                ctx.Response.ContentType = "application/problem+json";
                await ctx.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Title = "An error occurred while processing your request.",
                    Status = 500,
                    Extensions = { ["traceId"] = traceId }
                });
            });
        });

        app.UseStatusCodePages();

        return app;
    }
}

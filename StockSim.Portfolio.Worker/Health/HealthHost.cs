using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

public sealed class HealthHost : BackgroundService
{
    private IHost? _web;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _web = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(wb =>
            {
                wb.UseKestrel(o => o.ListenAnyIP(9082));
                wb.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/healthz", () => Results.Ok("ok"));
                        endpoints.MapGet("/readyz", () => Results.Ok("ready"));
                    });
                });
            })
            .Build();

        await _web.StartAsync(stoppingToken);
        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { /* ignore */ }
        await _web.StopAsync();
    }
}

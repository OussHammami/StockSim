using Microsoft.Extensions.Diagnostics.HealthChecks;
using StockSim.Infrastructure.Messaging;

namespace StockSim.Web.Health
{
    public class RabbitHealthCheck : IHealthCheck
    {
        private readonly RabbitConnection _rc;
        public RabbitHealthCheck(RabbitConnection rc) => _rc = rc;

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext ctx, CancellationToken ct = default)
        {
            try { using var ch = _rc.Connection.CreateModel(); return Task.FromResult(HealthCheckResult.Healthy()); }
            catch (Exception ex) { return Task.FromResult(HealthCheckResult.Unhealthy(exception: ex)); }
        }
    }
}

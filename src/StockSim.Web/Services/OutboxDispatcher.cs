using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StockSim.Application.Contracts.Orders;
using StockSim.Infrastructure.Messaging;
using StockSim.Infrastructure.Persistence;
using StockSim.Infrastructure.Persistence.Entities;
using System.Text.Json;

public sealed class OutboxDispatcher(IServiceProvider sp, ILogger<OutboxDispatcher> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hub = scope.ServiceProvider.GetRequiredService<IHubContext<OrderHub>>();

            var batch = await db.Set<OutboxMessage>()
                .Where(m => m.ProcessedUtc == null)
                .OrderBy(m => m.OccurredUtc)
                .Take(50)
                .ToListAsync(ct);

            foreach (var m in batch)
            {
                try
                {
                    if (m.Type == nameof(OrderFilledEvent))
                    {
                        var e = JsonSerializer.Deserialize<OrderFilledEvent>(m.Payload)!;
                        await hub.Clients.Group($"u:{e.UserId}").SendAsync("order", new
                        {
                            e.OrderId,
                            e.Symbol,
                            e.Quantity,
                            e.FillPrice,
                            Status = "Filled",
                            e.TimeUtc
                        }, ct);
                    }
                    else if (m.Type == nameof(OrderRejectedEvent))
                    {
                        var e = JsonSerializer.Deserialize<OrderRejectedEvent>(m.Payload)!;
                        await hub.Clients.Group($"u:{e.UserId}").SendAsync("order", new
                        {
                            e.OrderId,
                            e.Symbol,
                            e.Quantity,
                            Status = "Rejected",
                            e.Reason,
                            e.TimeUtc
                        }, ct);
                    }
                    else if (m.Type == nameof(OrderAcceptedEvent))
                    {
                        var e = JsonSerializer.Deserialize<OrderAcceptedEvent>(m.Payload)!;
                        await hub.Clients.Group($"u:{e.UserId}").SendAsync("order", new
                        {
                            e.OrderId,
                            e.UserId,
                            e.Symbol,
                            e.Quantity,
                            Status = "Pending",
                            FillPrice = (decimal?)null,
                            Reason = (string?)null,
                            e.TimeUtc
                        }, ct);
                    }

                    m.ProcessedUtc = DateTimeOffset.UtcNow;
                }
                catch (Exception ex)
                {
                    m.Attempts++;
                    log.LogError(ex, "Outbox dispatch failed for {Id}", m.Id);
                }
            }
            await db.SaveChangesAsync(ct);
            await Task.Delay(500, ct);
        }
    }
}

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace StockSim.Infrastructure.Messaging
{
    public sealed class OrderHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // client passes ?userId=...; we group by user
            var userId = Context.GetHttpContext()!.Request.Query["userId"].ToString();
            if (!string.IsNullOrWhiteSpace(userId))
                await Groups.AddToGroupAsync(Context.ConnectionId, $"u:{userId}");
            await base.OnConnectedAsync();
        }
    }
}

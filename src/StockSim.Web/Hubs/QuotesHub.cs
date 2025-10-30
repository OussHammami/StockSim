using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StockSim.Domain.ValueObjects;

namespace StockSim.Web.Hubs;

[Authorize]
public sealed class QuotesHub : Hub
{
    
    public Task Subscribe(string symbol) =>
        Groups.AddToGroupAsync(Context.ConnectionId, Symbol.From(symbol).Value);

    public Task Unsubscribe(string symbol) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, Symbol.From(symbol).Value);
}

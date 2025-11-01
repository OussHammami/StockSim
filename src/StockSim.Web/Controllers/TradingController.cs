using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSim.Application.Orders;
using StockSim.Application.Orders.Commands;
using StockSim.Domain.ValueObjects;
using StockSim.Web.Auth;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace StockSim.Web.Controllers;

[ApiController]
[Route("api/trading")]
public sealed class TradingController : ControllerBase
{
    private readonly IOrderService _orders;

    public TradingController(IOrderService orders) => _orders = orders;

    [HttpPost("orders")]
    [Authorize]
    public async Task<ActionResult<string>> Place([FromBody] PlaceOrderDto dto, CancellationToken ct)
    {
        var userId = dto.UserId ?? User.GetStableUserId();
        var id = await _orders.PlaceAsync(new PlaceOrder(
            userId,
            dto.Symbol,
            dto.Side,
            dto.Type,
            dto.Quantity,
            dto.LimitPrice), ct);

        return CreatedAtAction(nameof(GetById), new { id = id.ToString() }, id.ToString());
    }

    [HttpPost("orders/{id}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel([FromRoute] string id, [FromBody] CancelOrderDto dto, CancellationToken ct)
    {
        var userId = dto.UserId ?? User.GetStableUserId();
        await _orders.CancelAsync(new CancelOrder(userId, OrderId.From(Guid.Parse(id)), dto.Reason), ct);
        return NoContent();
    }

    [HttpGet("orders/{id}")]
    [Authorize]
    public async Task<IActionResult> GetById([FromRoute] string id, CancellationToken ct)
    {
        var order = await _orders.GetAsync(OrderId.From(Guid.Parse(id)), ct);
        if (order is null) return NotFound();

        return Ok(new
        {
            Id = order.Id.ToString(),
            order.UserId,
            Symbol = order.Symbol.Value,
            order.Side,
            order.Type,
            Quantity = order.Quantity.Value,
            LimitPrice = order.LimitPrice?.Value,
            order.State,
            order.FilledQuantity,
            order.AverageFillPrice
        });
    }

    public sealed record PlaceOrderDto(
        Guid? UserId,
        string Symbol,
        Domain.Orders.OrderSide Side,
        Domain.Orders.OrderType Type,
        decimal Quantity,
        decimal? LimitPrice);

    public sealed record CancelOrderDto(Guid? UserId, string? Reason);
}

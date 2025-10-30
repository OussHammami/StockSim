using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSim.Application.Portfolios;
using StockSim.Domain.ValueObjects;

namespace StockSim.Web.Controllers;

[ApiController]
[Route("api/portfolio")]
public sealed class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _svc;
    public PortfolioController(IPortfolioService svc) => _svc = svc;

    [HttpGet("summary")]
    [Authorize]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var userId = GetUserId();
        var p = await _svc.GetOrCreateAsync(userId, ct);

        return Ok(new
        {
            PortfolioId = p.Id.ToString(),
            p.UserId,
            Cash = p.Cash.Amount,
            ReservedCash = p.ReservedCash.Amount,
            Positions = p.Positions.Select(x => new {
                Symbol = x.Symbol.Value,
                x.Quantity,
                x.AvgCost
            })
        });
    }

    [HttpPost("deposit")]
    [Authorize]
    public async Task<IActionResult> Deposit([FromBody] DepositDto dto, CancellationToken ct)
    {
        var userId = dto.UserId ?? GetUserId();
        await _svc.DepositAsync(userId, Money.From(dto.Amount), ct);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst("uid")?.Value;
        return Guid.TryParse(sub, out var id) ? id : throw new InvalidOperationException("User id not found.");
    }

    public sealed record DepositDto(decimal Amount, Guid? UserId);
}

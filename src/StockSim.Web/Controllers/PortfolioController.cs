using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockSim.Application.Portfolios;
using StockSim.Domain.ValueObjects;
using StockSim.Web.Auth;
using System.Security.Claims;

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
        var userId = User.GetStableUserId();
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
        var userId = dto.UserId ?? User.GetStableUserId();
        await _svc.DepositAsync(userId, Money.From(dto.Amount), ct);
        return NoContent();
    }

    public sealed record DepositDto(decimal Amount, Guid? UserId);
}

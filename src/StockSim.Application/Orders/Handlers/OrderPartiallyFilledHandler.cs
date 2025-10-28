using StockSim.Application.Abstractions.Events;
using StockSim.Application.Portfolios;
using StockSim.Domain.Orders.Events;

namespace StockSim.Application.Orders.Handlers;

public sealed class OrderPartiallyFilledHandler : IDomainEventHandler<OrderPartiallyFilled>
{
    private readonly IPortfolioRepository _repo;
    private readonly Func<Guid> _userIdResolver; // supply from app service if needed

    public OrderPartiallyFilledHandler(IPortfolioRepository repo)
        : this(repo, () => Guid.Empty) { }

    public OrderPartiallyFilledHandler(IPortfolioRepository repo, Func<Guid> userIdResolver)
    {
        _repo = repo;
        _userIdResolver = userIdResolver;
    }

    public async Task HandleAsync(OrderPartiallyFilled e, CancellationToken ct = default)
    {
        // In real flow, resolve portfolio by order ownership. Here use resolver hook.
        var userId = _userIdResolver();
        var p = await _repo.GetByUserAsync(userId, ct).ConfigureAwait(false);
        if (p is null) return;

        // We do not know side or symbol here without loading the Order.
        // For the demo pipeline, assume BUY on a synthetic symbol hook.
        // Replace later when Order is accessible.
    }
}

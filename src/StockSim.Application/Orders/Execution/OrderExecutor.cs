using StockSim.Application.Abstractions.Events;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Orders.Execution;

public sealed class OrderExecutor : IOrderExecutor
{
    private readonly IOrderRepository _orders;
    private readonly IMatchingEngine _engine;
    private readonly IQuoteSnapshotProvider _quotes;
    private readonly IEventDispatcher _dispatcher;
    private readonly IIntegrationEventMapper _mapper;
    private readonly IOutboxWriter<ITradingOutboxContext> _outbox;

    public OrderExecutor(
        IOrderRepository orders,
        IMatchingEngine engine,
        IQuoteSnapshotProvider quotes,
        IEventDispatcher dispatcher,
        IIntegrationEventMapper mapper,
        IOutboxWriter<ITradingOutboxContext> outbox)
    {
        _orders = orders;
        _engine = engine;
        _quotes = quotes;
        _dispatcher = dispatcher;
        _mapper = mapper;
        _outbox = outbox;
    }

    public async Task ExecuteForSymbolAsync(string symbol, CancellationToken ct = default)
    {
        var snap = _quotes.Get(symbol);
        if (snap is null) return;

        var symVO = Symbol.From(symbol);
        var open = await _orders.GetOpenBySymbolAsync(symVO, ct).ConfigureAwait(false);
        if (open.Count == 0) return;

        var fills = _engine.Match(symVO, snap, open).ToArray();
        if (fills.Length == 0) return;

        foreach (var f in fills)
        {
            f.Order.ApplyFill(Quantity.From(f.FillQty), Price.From(f.FillPrice));
        }

        var domainEvents = open.SelectMany(o => o.DomainEvents).ToArray();
        await _dispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        var integrationEvents = _mapper.Map(domainEvents).ToArray();
        foreach (var o in open) o.ClearDomainEvents();

        await _outbox.WriteAsync(integrationEvents, ct).ConfigureAwait(false);
        await _orders.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    // Optional periodic sweep if you want to run through all symbols
    public async Task SweepAllAsync(CancellationToken ct = default)
    {
        // In v1 you might track active symbols in memory when orders are placed.
        // For a simple start: get distinct symbols from open orders (add repository method).
        // Placeholder no-op.
    }
}
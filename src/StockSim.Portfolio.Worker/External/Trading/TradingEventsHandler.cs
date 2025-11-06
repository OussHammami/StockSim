using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
using StockSim.Application.Portfolios;
using StockSim.Contracts.Trading;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;

namespace StockSim.Portfolio.Worker.External.Trading;

/// <summary>
/// Consumes Trading integration events and applies changes to the Portfolio aggregate.
/// Emits Portfolio integration events via the Portfolio outbox.
/// </summary>
public sealed class TradingEventsHandler : ITradingEventHandler
{
    private readonly ILogger<TradingEventsHandler> _log;
    private readonly IPortfolioRepository _portfolios;
    private readonly IIntegrationEventMapper _mapper;
    private readonly IOutboxWriter<IPortfolioOutboxContext> _outbox;

    public TradingEventsHandler(
        ILogger<TradingEventsHandler> log,
        IPortfolioRepository portfolios,
        IIntegrationEventMapper mapper,
        IOutboxWriter<IPortfolioOutboxContext> outbox)
    {
        _log = log;
        _portfolios = portfolios;
        _mapper = mapper;
        _outbox = outbox;
    }

    public async Task HandleAsync(
        string type,
        string dataJson,
        IReadOnlyDictionary<string, string?> headers,
        CancellationToken ct)
    {
        switch (type)
        {
            case "trading.order.accepted":
            case "trading.order.accepted.v1":
                await OnOrderAcceptedAsync(Parse<OrderAcceptedV1>(dataJson), ct);
                return;

            // NOTE: Your Trading v1 contracts for fills (OrderPartiallyFilledV1, OrderFilledV1)
            // do not include userId/symbol/side. Without those the Portfolio context cannot
            // compute the effect safely. Handle acceptance now; extend fill events later.
            case "trading.order.partiallyfilled":
            case "trading.order.partiallyfilled.v1":
            case "trading.order.filled":
            case "trading.order.filled.v1":
                _log.LogWarning("Fill event ignored due to insufficient payload for Portfolio: {Type}", type);
                return;

            default:
                _log.LogWarning("Unhandled event type: {Type}", type);
                return;
        }
    }

    private async Task OnOrderAcceptedAsync(OrderAcceptedV1 e, CancellationToken ct)
    {
        // parse ids from strings
        var orderIdGuid = ParseGuid(e.OrderId, nameof(e.OrderId));
        var userIdGuid = ParseGuid(e.UserId, nameof(e.UserId));

        // Load or create portfolio by user
        var p = await _portfolios.GetByUserAsync(userIdGuid, ct);
        if (p is null)
        {
            p = new Domain.Portfolio.Portfolio(PortfolioId.New(), userIdGuid);
            await _portfolios.AddAsync(p, ct);
        }

        // Reserve based on side/type (mirrors your previous in-process handler)
        var side = Enum.Parse<OrderSide>(e.Side, ignoreCase: true);
        var type = Enum.Parse<OrderType>(e.Type, ignoreCase: true);

        if (side == OrderSide.Buy && type == OrderType.Limit && e.LimitPrice is decimal lp)
        {
            var cost = Money.From(e.Quantity * lp);
            p.ReserveFunds(OrderId.From(orderIdGuid), cost);
        }
        else if (side == OrderSide.Sell)
        {
            p.ReserveShares(
                OrderId.From(orderIdGuid),
                Symbol.From(e.Symbol),
                Quantity.From(e.Quantity));
        }

        // Publish resulting Portfolio domain events as integration events
        var ievents = _mapper.Map(p.DomainEvents);
        p.ClearDomainEvents();

        await _outbox.WriteAsync(ievents, ct);
        await _portfolios.SaveChangesAsync(ct);

        _log.LogInformation("Processed trading.order.accepted for order {OrderId} user {UserId}", e.OrderId, e.UserId);
    }
    private static Guid ParseGuid(string value, string name) =>
        Guid.TryParse(value, out var g) ? g : throw new ArgumentException($"Invalid GUID for '{name}': '{value}'");
    private static T Parse<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new ArgumentException($"Invalid {typeof(T).Name} payload");
}

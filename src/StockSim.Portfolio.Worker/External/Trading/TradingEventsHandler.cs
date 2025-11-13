using Microsoft.Extensions.Logging;
using StockSim.Application.Abstractions.Outbox;
using StockSim.Application.Integration;
using StockSim.Application.Portfolios;
using StockSim.Contracts.Trading;
using StockSim.Contracts.Trading.V1;
using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;
using System.Text.Json;

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
    private readonly IMarketPriceProvider _prices;

    public TradingEventsHandler(
        ILogger<TradingEventsHandler> log,
        IPortfolioRepository portfolios,
        IIntegrationEventMapper mapper,
        IOutboxWriter<IPortfolioOutboxContext> outbox,
        IMarketPriceProvider prices)
    {
        _log = log;
        _portfolios = portfolios;
        _mapper = mapper;
        _outbox = outbox;
        _prices = prices;
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

            // Prefer v2 fill events (see Part B)
            case "trading.order.partiallyFilled.v2":
                await OnOrderPartiallyFilledV2Async(Parse<Contracts.Trading.V2.OrderPartiallyFilledV2>(dataJson), ct);
                return;

            case "trading.order.filled.v2":
                await OnOrderFilledV2Async(Parse<Contracts.Trading.V2.OrderFilledV2>(dataJson), ct);
                return;

            case "trading.order.partiallyfilled":
            case "trading.order.partiallyfilled.v1":
            case "trading.order.filled":
            case "trading.order.filled.v1":
                _log.LogWarning("Ignored legacy fill event with insufficient payload: {Type}", type);
                return;

            default:
                _log.LogWarning("Unhandled event type: {Type}", type);
                return;
        }
    }

    private async Task OnOrderAcceptedAsync(OrderAcceptedV1 e, CancellationToken ct)
    {
        var orderIdGuid = ParseGuid(e.OrderId, nameof(e.OrderId));
        var userIdGuid = ParseGuid(e.UserId, nameof(e.UserId));

        var p = await _portfolios.GetByUserAsync(userIdGuid, ct);
        if (p is null)
        {
            p = new Domain.Portfolio.Portfolio(PortfolioId.New(), userIdGuid);
            await _portfolios.AddAsync(p, ct);
        }

        var side = Enum.Parse<OrderSide>(e.Side, ignoreCase: true);
        var type = Enum.Parse<OrderType>(e.Type, ignoreCase: true);

        if (side == OrderSide.Buy)
        {
            if (type == OrderType.Limit && e.LimitPrice is decimal lp)
            {
                var cost = Money.From(e.Quantity * lp);
                p.ReserveFunds(OrderId.From(orderIdGuid), cost);
            }
            else if (type == OrderType.Market)
            {
                // Estimate using current Ask * buffer (e.g., 1% by default)
                var ask = await _prices.GetAskAsync(e.Symbol, ct) ?? 0m;
                var bufferPct = 0.01m; // make configurable if desired
                var estPx = ask > 0 ? ask * (1 + bufferPct) : 0m;

                if (estPx > 0)
                {
                    var estCost = Money.From(e.Quantity * estPx);
                    p.ReserveFunds(OrderId.From(orderIdGuid), estCost);
                    _log.LogInformation("Reserved funds for BUY Market {OrderId}: qty {Qty} estPx {Px} (ask {Ask}, buffer {Buf:P2})",
                        e.OrderId, e.Quantity, estPx, ask, bufferPct);
                }
                else
                {
                    _log.LogWarning("No ask available to reserve funds for BUY Market {OrderId} on {Symbol}. Skipping reservation.", e.OrderId, e.Symbol);
                }
            }
        }
        else if (side == OrderSide.Sell)
        {
            p.ReserveShares(
                OrderId.From(orderIdGuid),
                Symbol.From(e.Symbol),
                Quantity.From(e.Quantity));
        }

        var ievents = _mapper.Map(p.DomainEvents);
        p.ClearDomainEvents();

        await _outbox.WriteAsync(ievents, ct);
        await _portfolios.SaveChangesAsync(ct);

        _log.LogInformation("Processed trading.order.accepted for order {OrderId} user {UserId}", e.OrderId, e.UserId);
    }

    private async Task OnOrderPartiallyFilledV2Async(Contracts.Trading.V2.OrderPartiallyFilledV2 e, CancellationToken ct)
    {
        var orderId = OrderId.From(ParseGuid(e.OrderId, nameof(e.OrderId)));
        var userId = ParseGuid(e.UserId, nameof(e.UserId));
        var p = await _portfolios.GetByUserAsync(userId, ct)
                ?? new Domain.Portfolio.Portfolio(PortfolioId.New(), userId).Also(async x => await _portfolios.AddAsync(x, ct));

        var side = Enum.Parse<OrderSide>(e.Side, ignoreCase: true);
        p.ApplyFill(orderId, side, Symbol.From(e.Symbol), Quantity.From(e.FillQuantity), Price.From(e.FillPrice));

        var ievents = _mapper.Map(p.DomainEvents);
        p.ClearDomainEvents();

        await _outbox.WriteAsync(ievents, ct);
        await _portfolios.SaveChangesAsync(ct);

        _log.LogInformation("Processed trading.order.partiallyFilled.v2 for order {OrderId} qty {Qty} px {Px}", e.OrderId, e.FillQuantity, e.FillPrice);
    }

    private async Task OnOrderFilledV2Async(Contracts.Trading.V2.OrderFilledV2 e, CancellationToken ct)
    {
        var orderId = OrderId.From(ParseGuid(e.OrderId, nameof(e.OrderId)));
        var userId = ParseGuid(e.UserId, nameof(e.UserId));
        var p = await _portfolios.GetByUserAsync(userId, ct)
                ?? new Domain.Portfolio.Portfolio(PortfolioId.New(), userId).Also(async x => await _portfolios.AddAsync(x, ct));

        // Typically the last tranche is already applied via partials; ensure reservation release happens via Portfolio rules.

        var ievents = _mapper.Map(p.DomainEvents);
        p.ClearDomainEvents();

        await _outbox.WriteAsync(ievents, ct);
        await _portfolios.SaveChangesAsync(ct);

        _log.LogInformation("Processed trading.order.filled.v2 for order {OrderId}", e.OrderId);
    }

    private static Guid ParseGuid(string value, string name) =>
        Guid.TryParse(value, out var g) ? g : throw new ArgumentException($"Invalid GUID for '{name}': '{value}'");

    private static T Parse<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new ArgumentException($"Invalid {typeof(T).Name} payload");
}

internal static class Extensions
{
    public static T Also<T>(this T value, Func<T, Task> action)
    {
        action(value).GetAwaiter().GetResult();
        return value;
    }
}
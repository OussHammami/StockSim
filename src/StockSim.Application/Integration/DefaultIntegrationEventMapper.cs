using StockSim.Contracts.Common;
using StockSim.Contracts.Portfolio;
using StockSim.Contracts.Trading;
using StockSim.Domain.Orders.Events;
using StockSim.Domain.Portfolio.Events;
using StockSim.Domain.Primitives;

namespace StockSim.Application.Integration;

public sealed class DefaultIntegrationEventMapper : IIntegrationEventMapper
{
    public IEnumerable<IntegrationEvent> Map(IEnumerable<IDomainEvent> domainEvents)
    {
        foreach (var e in domainEvents)
        {
            switch (e)
            {
                case OrderAccepted oa:
                {
                    var payload = new OrderAcceptedV1(
                        OrderId: oa.OrderId.ToString(),
                        UserId: oa.UserId,
                        Symbol: oa.Symbol.Value,
                        Side: oa.Side.ToString(),
                        Type: oa.Type.ToString(),
                        Quantity: oa.Quantity,
                        LimitPrice: oa.LimitPrice
                    );

                    var env = new EnvelopeV1(
                        Id: Guid.NewGuid().ToString("N"),
                        Type: "trading.order.accepted",
                        Source: "trading",
                        Subject: oa.OrderId.ToString(),
                        OccurredAt: oa.OccurredAt,
                        SchemaVersion: "1",
                        DedupeKey: $"trading|order.accepted|{oa.OrderId}"
                    );

                    yield return IntegrationEvent.Create(
                        type: env.Type, source: env.Source, subject: env.Subject,
                        data: payload, occurredAt: env.OccurredAt, dedupeKey: env.DedupeKey);
                    break;
                }

                case OrderPartiallyFilled opf:
                {
                    var payload = new OrderPartiallyFilledV1(
                        opf.OrderId.ToString(), opf.FillQuantity, opf.FillPrice, opf.CumFilledQuantity);

                    var env = new EnvelopeV1(
                        Guid.NewGuid().ToString("N"), "trading.order.partiallyFilled", "trading",
                        opf.OrderId.ToString(), opf.OccurredAt, "1",
                        $"trading|order.partiallyFilled|{opf.OrderId}|{opf.CumFilledQuantity}");

                    yield return IntegrationEvent.Create(env.Type, env.Source, env.Subject, payload, env.OccurredAt, env.DedupeKey);
                    break;
                }

                case OrderFilled of:
                {
                    var payload = new OrderFilledV1(of.OrderId.ToString(), of.TotalFilledQuantity, of.AverageFillPrice);
                    var env = new EnvelopeV1(
                        Guid.NewGuid().ToString("N"), "trading.order.filled", "trading",
                        of.OrderId.ToString(), of.OccurredAt, "1",
                        $"trading|order.filled|{of.OrderId}");
                    yield return IntegrationEvent.Create(env.Type, env.Source, env.Subject, payload, env.OccurredAt, env.DedupeKey);
                    break;
                }

                case OrderRejected orj:
                {
                    var payload = new OrderRejectedV1(orj.OrderId.ToString(), orj.Reason);
                    var env = new EnvelopeV1(
                        Guid.NewGuid().ToString("N"), "trading.order.rejected", "trading",
                        orj.OrderId.ToString(), orj.OccurredAt, "1",
                        $"trading|order.rejected|{orj.OrderId}");
                    yield return IntegrationEvent.Create(env.Type, env.Source, env.Subject, payload, env.OccurredAt, env.DedupeKey);
                    break;
                }

                case FundsReserved fr:
                {
                    var payload = new FundsReservedV1(fr.PortfolioId.ToString(), fr.OrderId.ToString(), fr.Amount.Amount);
                    var env = new EnvelopeV1(
                        Guid.NewGuid().ToString("N"), "portfolio.funds.reserved", "portfolio",
                        fr.PortfolioId.ToString(), fr.OccurredAt, "1",
                        $"portfolio|funds.reserved|{fr.PortfolioId}|{fr.OrderId}");
                    yield return IntegrationEvent.Create(env.Type, env.Source, env.Subject, payload, env.OccurredAt, env.DedupeKey);
                    break;
                }

                case SharesReserved sr:
                {
                    var payload = new SharesReservedV1(sr.PortfolioId.ToString(), sr.OrderId.ToString(), sr.Symbol.Value, sr.Quantity.Value);
                    var env = new EnvelopeV1(
                        Guid.NewGuid().ToString("N"), "portfolio.shares.reserved", "portfolio",
                        sr.PortfolioId.ToString(), sr.OccurredAt, "1",
                        $"portfolio|shares.reserved|{sr.PortfolioId}|{sr.OrderId}");
                    yield return IntegrationEvent.Create(env.Type, env.Source, env.Subject, payload, env.OccurredAt, env.DedupeKey);
                    break;
                }

                case ReservationReleased rr:
                {
                    var payload = new ReservationReleasedV1(
                        rr.PortfolioId.ToString(), rr.OrderId.ToString(),
                        rr.Funds?.Amount, rr.Symbol?.Value, rr.Shares?.Value, rr.Reason);
                    var env = new EnvelopeV1(
                        Guid.NewGuid().ToString("N"), "portfolio.reservation.released", "portfolio",
                        rr.PortfolioId.ToString(), rr.OccurredAt, "1",
                        $"portfolio|reservation.released|{rr.PortfolioId}|{rr.OrderId}|{rr.Reason}");
                    yield return IntegrationEvent.Create(env.Type, env.Source, env.Subject, payload, env.OccurredAt, env.DedupeKey);
                    break;
                }

                case FillApplied fa:
                {
                    var payload = new FillAppliedV1(
                        fa.PortfolioId.ToString(), fa.OrderId.ToString(), fa.Side.ToString(), fa.Symbol.Value,
                        fa.Quantity.Value, fa.Price.Value, fa.CashDelta.Amount, fa.NewPositionQty, fa.NewAvgCost);
                    var env = new EnvelopeV1(
                        Guid.NewGuid().ToString("N"), "portfolio.fill.applied", "portfolio",
                        fa.PortfolioId.ToString(), fa.OccurredAt, "1",
                        $"portfolio|fill.applied|{fa.PortfolioId}|{fa.OrderId}|{fa.Quantity.Value}");
                    yield return IntegrationEvent.Create(env.Type, env.Source, env.Subject, payload, env.OccurredAt, env.DedupeKey);
                    break;
                }
            }
        }
    }
}

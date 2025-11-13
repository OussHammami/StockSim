using StockSim.Application.Orders;
using StockSim.Contracts.Common;
using StockSim.Contracts.Portfolio;
using StockSim.Contracts.Trading.V1;
using StockSim.Contracts.Trading.V2;
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
                            UserId: oa.UserId.ToString(),
                            Symbol: oa.Symbol.Value,
                            Side: oa.Side.ToString(),
                            Type: oa.Type.ToString(),
                            Quantity: oa.Quantity,
                            LimitPrice: oa.LimitPrice,
                            OccurredAt: oa.OccurredAt
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
                            OrderId: opf.OrderId.ToString(),
                            FillQuantity: opf.FillQuantity,
                            FillPrice: opf.FillPrice,
                            CumFilledQuantity: opf.CumFilledQuantity);

                        var env = new EnvelopeV1(
                            Id: Guid.NewGuid().ToString("N"),
                            Type: "trading.order.partiallyFilled",
                            Source: "trading",
                            Subject: opf.OrderId.ToString(),
                            OccurredAt: opf.OccurredAt,
                            SchemaVersion: "1",
                            DedupeKey: $"trading|order.partiallyFilled|{opf.OrderId}|{opf.CumFilledQuantity}");

                        yield return IntegrationEvent.Create(
                            type: env.Type, source: env.Source, subject: env.Subject,
                            data: payload, occurredAt: env.OccurredAt, dedupeKey: env.DedupeKey);

                        var payloadV2 = new OrderPartiallyFilledV2(
                            OrderId: opf.OrderId.ToString(),
                            UserId: opf.UserId.ToString(),
                            Symbol: opf.Symbol.Value,
                            Side: opf.Side.ToString(),
                            FillQuantity: opf.FillQuantity,
                            FillPrice: opf.FillPrice,
                            CumFilledQuantity: opf.CumFilledQuantity,
                            OccurredAt: opf.OccurredAt);

                        var envV2 = new EnvelopeV1(
                            Id: Guid.NewGuid().ToString("N"),
                            Type: "trading.order.partiallyFilled.v2",
                            Source: "trading",
                            Subject: opf.OrderId.ToString(),
                            OccurredAt: opf.OccurredAt,
                            SchemaVersion: "2",
                            DedupeKey: $"trading|order.partiallyFilled.v2|{opf.OrderId}|{opf.CumFilledQuantity}");

                        yield return IntegrationEvent.Create(
                            type: envV2.Type, source: envV2.Source, subject: envV2.Subject,
                            data: payloadV2, occurredAt: envV2.OccurredAt, dedupeKey: envV2.DedupeKey);
                        break;
                    }

                case OrderFilled of:
                    {
                        var payload = new OrderFilledV1(
                            OrderId: of.OrderId.ToString(),
                            TotalFilledQuantity: of.TotalFilledQuantity,
                            AverageFillPrice: of.AverageFillPrice);

                        var env = new EnvelopeV1(
                            Id: Guid.NewGuid().ToString("N"),
                            Type: "trading.order.filled",
                            Source: "trading",
                            Subject: of.OrderId.ToString(),
                            OccurredAt: of.OccurredAt,
                            SchemaVersion: "1",
                            DedupeKey: $"trading|order.filled|{of.OrderId}");

                        yield return IntegrationEvent.Create(
                            type: env.Type, source: env.Source, subject: env.Subject,
                            data: payload, occurredAt: env.OccurredAt, dedupeKey: env.DedupeKey);

                        var payloadV2 = new OrderFilledV2(
                            OrderId: of.OrderId.ToString(),
                            UserId: of.UserId.ToString(),
                            Symbol: of.Symbol.Value,
                            Side: of.Side.ToString(),
                            TotalFilledQuantity: of.TotalFilledQuantity,
                            AverageFillPrice: of.AverageFillPrice,
                            OccurredAt: of.OccurredAt);

                        var envV2 = new EnvelopeV1(
                            Id: Guid.NewGuid().ToString("N"),
                            Type: "trading.order.filled.v2",
                            Source: "trading",
                            Subject: of.OrderId.ToString(),
                            OccurredAt: of.OccurredAt,
                            SchemaVersion: "2",
                            DedupeKey: $"trading|order.filled.v2|{of.OrderId}");

                        yield return IntegrationEvent.Create(
                            type: envV2.Type, source: envV2.Source, subject: envV2.Subject,
                            data: payloadV2, occurredAt: envV2.OccurredAt, dedupeKey: envV2.DedupeKey);
                        break;
                    }

                case OrderRejected orj:
                    {
                        var payload = new OrderRejectedV1(orj.OrderId.ToString(), orj.Reason);
                        var env = new EnvelopeV1(
                            Id: Guid.NewGuid().ToString("N"),
                            Type: "trading.order.rejected",
                            Source: "trading",
                            Subject: orj.OrderId.ToString(),
                            OccurredAt: orj.OccurredAt,
                            SchemaVersion: "1",
                            DedupeKey: $"trading|order.rejected|{orj.OrderId}");

                        yield return IntegrationEvent.Create(
                            type: env.Type, source: env.Source, subject: env.Subject,
                            data: payload, occurredAt: env.OccurredAt, dedupeKey: env.DedupeKey);
                        break;
                    }

                case FundsReserved fr:
                    {
                        var payload = new FundsReservedV1(fr.PortfolioId.ToString(), fr.OrderId.ToString(), fr.Amount.Amount);
                        var env = new EnvelopeV1(
                            Id: Guid.NewGuid().ToString("N"),
                            Type: "portfolio.funds.reserved",
                            Source: "portfolio",
                            Subject: fr.PortfolioId.ToString(),
                            OccurredAt: fr.OccurredAt,
                            SchemaVersion: "1",
                            DedupeKey: $"portfolio|funds.reserved|{fr.PortfolioId}|{fr.OrderId}");

                        yield return IntegrationEvent.Create(
                            type: env.Type, source: env.Source, subject: env.Subject,
                            data: payload, occurredAt: env.OccurredAt, dedupeKey: env.DedupeKey);
                        break;
                    }

                case SharesReserved sr:
                    {
                        var payload = new SharesReservedV1(sr.PortfolioId.ToString(), sr.OrderId.ToString(), sr.Symbol.Value, sr.Quantity.Value);
                        var env = new EnvelopeV1(
                            Id: Guid.NewGuid().ToString("N"),
                            Type: "portfolio.shares.reserved",
                            Source: "portfolio",
                            Subject: sr.PortfolioId.ToString(),
                            OccurredAt: sr.OccurredAt,
                            SchemaVersion: "1",
                            DedupeKey: $"portfolio|shares.reserved|{sr.PortfolioId}|{sr.OrderId}");

                        yield return IntegrationEvent.Create(
                            type: env.Type, source: env.Source, subject: env.Subject,
                            data: payload, occurredAt: env.OccurredAt, dedupeKey: env.DedupeKey);
                        break;
                    }

                case ReservationReleased rr:
                    {
                        var payload = new ReservationReleasedV1(
                            PortfolioId: rr.PortfolioId.ToString(),
                            OrderId: rr.OrderId.ToString(),
                            Funds: rr.Funds?.Amount,
                            Symbol: rr.Symbol?.Value,
                            Shares: rr.Shares?.Value,
                            Reason: rr.Reason);

                        var env = new EnvelopeV1(
                            Id: Guid.NewGuid().ToString("N"),
                            Type: "portfolio.reservation.released",
                            Source: "portfolio",
                            Subject: rr.PortfolioId.ToString(),
                            OccurredAt: rr.OccurredAt,
                            SchemaVersion: "1",
                            DedupeKey: $"portfolio|reservation.released|{rr.PortfolioId}|{rr.OrderId}|{rr.Reason}");

                        yield return IntegrationEvent.Create(
                            type: env.Type, source: env.Source, subject: env.Subject,
                            data: payload, occurredAt: env.OccurredAt, dedupeKey: env.DedupeKey);
                        break;
                    }

                case FillApplied fa:
                    {
                        var payload = new FillAppliedV1(
                            PortfolioId: fa.PortfolioId.ToString(),
                            OrderId: fa.OrderId.ToString(),
                            Side: fa.Side.ToString(),
                            Symbol: fa.Symbol.Value,
                            Quantity: fa.Quantity.Value,
                            Price: fa.Price.Value,
                            CashDelta: fa.CashDelta.Amount,
                            NewPositionQty: fa.NewPositionQty,
                            NewAvgCost: fa.NewAvgCost);

                        var env = new EnvelopeV1(
                            Id: Guid.NewGuid().ToString("N"),
                            Type: "portfolio.fill.applied",
                            Source: "portfolio",
                            Subject: fa.PortfolioId.ToString(),
                            OccurredAt: fa.OccurredAt,
                            SchemaVersion: "1",
                            DedupeKey: $"portfolio|fill.applied|{fa.PortfolioId}|{fa.OrderId}|{fa.Quantity.Value}");

                        yield return IntegrationEvent.Create(
                            type: env.Type, source: env.Source, subject: env.Subject,
                            data: payload, occurredAt: env.OccurredAt, dedupeKey: env.DedupeKey);
                        break;
                    }
            }
        }
    }
}
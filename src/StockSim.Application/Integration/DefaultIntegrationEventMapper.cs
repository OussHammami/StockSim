using StockSim.Domain.Primitives;
using StockSim.Domain.Orders.Events;
using StockSim.Domain.Portfolio.Events;

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
                    yield return IntegrationEvent.Create(
                        type: "trading.order.accepted",
                        source: "trading",
                        subject: oa.OrderId.ToString(),
                        data: new { oa.UserId, OrderId = oa.OrderId.ToString(), Symbol = oa.Symbol.Value, oa.Side, oa.Type, oa.Quantity, oa.LimitPrice },
                        occurredAt: oa.OccurredAt,
                        dedupeKey: $"trading|order.accepted|{oa.OrderId}"
                    );
                    break;

                case OrderPartiallyFilled opf:
                    yield return IntegrationEvent.Create(
                        "trading.order.partiallyFilled",
                        "trading",
                        opf.OrderId.ToString(),
                        new { OrderId = opf.OrderId.ToString(), opf.FillQuantity, opf.FillPrice, opf.CumFilledQuantity },
                        opf.OccurredAt,
                        $"trading|order.partiallyFilled|{opf.OrderId}|{opf.CumFilledQuantity}"
                    );
                    break;

                case OrderFilled of:
                    yield return IntegrationEvent.Create(
                        "trading.order.filled",
                        "trading",
                        of.OrderId.ToString(),
                        new { OrderId = of.OrderId.ToString(), of.TotalFilledQuantity, of.AverageFillPrice },
                        of.OccurredAt,
                        $"trading|order.filled|{of.OrderId}"
                    );
                    break;

                case OrderRejected orj:
                    yield return IntegrationEvent.Create(
                        "trading.order.rejected",
                        "trading",
                        orj.OrderId.ToString(),
                        new { OrderId = orj.OrderId.ToString(), orj.Reason },
                        orj.OccurredAt,
                        $"trading|order.rejected|{orj.OrderId}"
                    );
                    break;

                case FundsReserved fr:
                    yield return IntegrationEvent.Create(
                        "portfolio.funds.reserved",
                        "portfolio",
                        fr.PortfolioId.ToString(),
                        new { PortfolioId = fr.PortfolioId.ToString(), OrderId = fr.OrderId.ToString(), Amount = fr.Amount.Amount },
                        fr.OccurredAt,
                        $"portfolio|funds.reserved|{fr.PortfolioId}|{fr.OrderId}"
                    );
                    break;

                case SharesReserved sr:
                    yield return IntegrationEvent.Create(
                        "portfolio.shares.reserved",
                        "portfolio",
                        sr.PortfolioId.ToString(),
                        new { PortfolioId = sr.PortfolioId.ToString(), OrderId = sr.OrderId.ToString(), Symbol = sr.Symbol.Value, Quantity = sr.Quantity.Value },
                        sr.OccurredAt,
                        $"portfolio|shares.reserved|{sr.PortfolioId}|{sr.OrderId}"
                    );
                    break;

                case ReservationReleased rr:
                    yield return IntegrationEvent.Create(
                        "portfolio.reservation.released",
                        "portfolio",
                        rr.PortfolioId.ToString(),
                        new
                        {
                            PortfolioId = rr.PortfolioId.ToString(),
                            OrderId = rr.OrderId.ToString(),
                            Funds = rr.Funds?.Amount,
                            Symbol = rr.Symbol?.Value,
                            Shares = rr.Shares?.Value,
                            rr.Reason
                        },
                        rr.OccurredAt,
                        $"portfolio|reservation.released|{rr.PortfolioId}|{rr.OrderId}|{rr.Reason}"
                    );
                    break;

                case FillApplied fa:
                    yield return IntegrationEvent.Create(
                        "portfolio.fill.applied",
                        "portfolio",
                        fa.PortfolioId.ToString(),
                        new
                        {
                            PortfolioId = fa.PortfolioId.ToString(),
                            OrderId = fa.OrderId.ToString(),
                            fa.Side,
                            Symbol = fa.Symbol.Value,
                            Quantity = fa.Quantity.Value,
                            Price = fa.Price.Value,
                            CashDelta = fa.CashDelta.Amount,
                            fa.NewPositionQty,
                            fa.NewAvgCost
                        },
                        fa.OccurredAt,
                        $"portfolio|fill.applied|{fa.PortfolioId}|{fa.OrderId}|{fa.Quantity.Value}"
                    );
                    break;
            }
        }
    }
}

using StockSim.Domain.Orders;
using StockSim.Domain.ValueObjects;

namespace StockSim.Application.Orders.Execution;

public interface IMatchingEngine
{
    IEnumerable<ProposedFill> Match(Symbol symbol, QuoteSnapshot quote, IEnumerable<Order> openOrders);
}

public sealed record ProposedFill(Order Order, decimal FillQty, decimal FillPrice);
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace StockSim.Application.Telemetry;

public static class Telemetry
{
    public const string OrdersSourceName = "StockSim.Orders";
    public const string PortfolioSourceName = "StockSim.Portfolio";

    public static readonly ActivitySource OrdersSource = new(OrdersSourceName);
    public static readonly ActivitySource PortfolioSource = new(PortfolioSourceName);

    public static readonly Meter OrdersMeter = new(OrdersSourceName);

    public static readonly Counter<long> OrdersPlaced = OrdersMeter.CreateCounter<long>("orders_placed");
    public static readonly Counter<long> OrdersCanceled = OrdersMeter.CreateCounter<long>("orders_canceled");
    public static readonly Counter<long> OutboxEventsWritten = OrdersMeter.CreateCounter<long>("outbox_events_written");
}

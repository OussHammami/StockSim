using System.Diagnostics.Metrics;

namespace StockSim.Application.Orders.Execution;

public static class ExecutionTelemetry
{
    public static readonly Meter Meter = new("StockSim.Execution", "1.0.0");

    public static readonly Counter<int> OrdersScanned = Meter.CreateCounter<int>("execution_orders_scanned");
    public static readonly Counter<int> OrdersFilled = Meter.CreateCounter<int>("execution_orders_filled");
    public static readonly Counter<double> QuantityFilled = Meter.CreateCounter<double>("execution_quantity_filled");
    public static readonly Histogram<double> ExecutionLatencyMs = Meter.CreateHistogram<double>("execution_latency_ms");
}
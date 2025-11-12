namespace StockSim.Domain.Orders;

public enum TimeInForce
{
    Gtc = 0,        // Good Till Cancelled
    Day = 1,        // Expires end of day (UTC) – simplistic
    Ioc = 2,        // Immediate-Or-Cancel (partial allowed; remainder canceled)
    Fok = 3         // Fill-Or-Kill (all-or-nothing, no partial)
}
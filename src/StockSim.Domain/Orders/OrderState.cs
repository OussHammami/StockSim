namespace StockSim.Domain.Orders;

public enum OrderState
{
    New = 0,
    Accepted = 1,
    PartiallyFilled = 2,
    Filled = 3,
    Rejected = 4,
    Canceled = 5
}

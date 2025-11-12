namespace StockSim.Application.Integration.Events.Trading;

public sealed record OrderPartiallyFilledIE(
    Guid OrderId,
    string Symbol,
    string Side,
    decimal FillQuantity,
    decimal FillPrice,
    decimal CumulativeFilled);
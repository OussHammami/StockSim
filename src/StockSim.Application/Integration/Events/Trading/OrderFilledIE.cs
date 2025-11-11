namespace StockSim.Application.Integration.Events.Trading;

public sealed record OrderFilledIE(
    Guid OrderId,
    string Symbol,
    string Side,
    decimal TotalFilledQuantity,
    decimal AverageFillPrice);
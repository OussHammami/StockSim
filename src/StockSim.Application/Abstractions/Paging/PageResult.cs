namespace StockSim.Application.Abstractions.Paging;

public sealed record PageResult<T>(IReadOnlyList<T> Items, int Total);

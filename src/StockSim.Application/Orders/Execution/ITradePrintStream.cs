namespace StockSim.Application.Orders.Execution
{
    /// <summary>
    /// Push-style subscription of trade prints (tape).
    /// </summary>
    public interface ITradePrintStream
    {
        IDisposable Subscribe(Func<TradePrint, Task> onPrint);
    }
}

using FluentAssertions;
using StockSim.Application.Orders.Execution;

public class SymbolLocksTests
{
    [Fact]
    public async Task Same_Symbol_Reuses_Semaphore()
    {
        var locks = new SymbolLocks();

        var gate1 = locks.For("AAPL");
        var gate2 = locks.For("aapl");

        gate1.Should().BeSameAs(gate2);

        await gate1.WaitAsync();
        var couldEnter = await gate2.WaitAsync(TimeSpan.FromMilliseconds(25));
        couldEnter.Should().BeFalse();

        gate1.Release();

        var acquiredAfterRelease = await gate2.WaitAsync(TimeSpan.FromMilliseconds(50));
        acquiredAfterRelease.Should().BeTrue();
        gate2.Release();
    }

    [Fact]
    public void Different_Symbols_Get_Distinct_Gates()
    {
        var locks = new SymbolLocks();

        var gateA = locks.For("MSFT");
        var gateB = locks.For("GOOG");

        gateA.Should().NotBeSameAs(gateB);
    }
}

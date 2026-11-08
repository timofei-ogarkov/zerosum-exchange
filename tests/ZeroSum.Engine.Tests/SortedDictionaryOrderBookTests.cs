namespace ZeroSum.Engine.Tests;

public class SortedDictionaryOrderBookTests : OrderBookTests
{
    protected override IOrderBook CreateBook()
    {
        return new SortedDictionaryOrderBook();
    }
}
namespace ZeroSum.Engine.Tests;

public class BitmapOrderBookTests : OrderBookTests
{
    protected override IOrderBook CreateBook()
    {
        return new BitmapOrderBook();
    }
}
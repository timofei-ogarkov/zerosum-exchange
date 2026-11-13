namespace ZeroSum.Engine.Tests;

public class BitmapOrderBookTests : OrderBookTests
{
    protected override IOrderBook CreateBook()
    {
        return new BitmapOrderBook();
    }
    [Fact]
    public void Add_PriceOutsideGrid_Throws()
    {
        var book = CreateBook();
        var buy1 = CreateOrder(Side.Buy, 50.00m, 1);
        var buy2 = CreateOrder(Side.Buy, 120.00m, 2);
        Assert.Throws<ArgumentException>(() => book.Add(buy1));
        Assert.Throws<ArgumentException>(() => book.Add(buy2));
    }
}

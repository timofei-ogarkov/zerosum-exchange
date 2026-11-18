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
        var buy1 = CreateOrder(Side.Buy, 3000.00m, 1);
        Assert.Throws<ArgumentException>(() => book.Add(buy1));
    }
    [Fact]
    public void BestPrices_FarApartLevels_UseDifferentSummaries()
    {
        var book = CreateBook();
        var buy1 = CreateOrder(Side.Buy, 1.00m, 1);
        var buy2 = CreateOrder(Side.Buy, 2500.00m, 2);
        var sell1 = CreateOrder(Side.Sell, 2600.00m, 3);
        book.Add(buy1);
        book.Add(buy2);
        book.Add(sell1);
        Assert.Equal(2500.00m, book.BestBid());
        Assert.Equal(2600.00m, book.BestAsk());
        book.Cancel(2);
        Assert.Equal(1.00m, book.BestBid());

    }
}

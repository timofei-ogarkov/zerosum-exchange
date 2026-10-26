namespace ZeroSum.Engine.Tests;
public class OrderBookTests
{
    private static IOrderBook CreateBook()
    {
        return new SortedDictionaryOrderBook();
    }
    private static Order CreateOrder(Side side, decimal price)
    {
        return new Order
        {
            Id = 1,
            TraderId = "anna",
            OrderSide = side,
            Type = OrderType.Limit,
            Price = price,
            Quantity = 5m,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
    [Fact]
    public void EmptyBook_HasNoBestPrices()
    {
        var book = CreateBook();
        Assert.Null(book.BestBid());
        Assert.Null(book.BestAsk());
    }
    [Fact]
    public void BestBid_IsHighestBuyPrice()
    {
        var book = CreateBook();
        book.Add(CreateOrder(Side.Buy, 99.00m));
        book.Add(CreateOrder(Side.Buy, 99.50m));
        Assert.Equal(99.50m, book.BestBid());
    }
    [Fact]
    public void BestAsk_IsLowestSellPrice()
    {
        var book = CreateBook();
        book.Add(CreateOrder(Side.Sell, 101.00m));
        book.Add(CreateOrder(Side.Sell, 100.50m));
        Assert.Equal(100.50m, book.BestAsk());
    }
    [Fact]
    public void Add_MarketOrder_Throws()
    {
        var book = CreateBook();
        var order = new Order
        {
            Id = 1,
            TraderId = "anna",
            OrderSide = Side.Buy,
            Type = OrderType.Market,
            Quantity = 5m,
            Timestamp = DateTimeOffset.UtcNow
        };
        Assert.Throws<ArgumentException>(() => book.Add(order));
    }
}
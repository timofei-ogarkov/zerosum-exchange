namespace ZeroSum.Engine.Tests;

public class OrderBookTests
{
    private static IOrderBook CreateBook()
    {
        return new SortedDictionaryOrderBook();
    }
    private static Order CreateOrder(Side side, decimal price, long id = 1, decimal quantity = 5m)
    {
        return new Order
        {
            Id = id,
            TraderId = "anna",
            OrderSide = side,
            Type = OrderType.Limit,
            Price = price,
            Quantity = quantity,
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
        book.Add(CreateOrder(Side.Buy, 99.00m, 1));
        book.Add(CreateOrder(Side.Buy, 99.50m, 2));
        Assert.Equal(99.50m, book.BestBid());
    }
    [Fact]
    public void BestAsk_IsLowestSellPrice()
    {
        var book = CreateBook();
        book.Add(CreateOrder(Side.Sell, 101.00m, 1));
        book.Add(CreateOrder(Side.Sell, 100.50m, 2));
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
    [Fact]
    public void Buy_CrossingSell_CreatesTrade()
    {
        var book = CreateBook();
        book.Add(CreateOrder(Side.Sell, 99.00m, 1));
        var trades = book.Add(CreateOrder(Side.Buy, 100.00m, 2));

        var trade = Assert.Single(trades);
        Assert.Equal(99.00m, trade.Price);
        Assert.Equal(5m, trade.Quantity);
        Assert.Equal(2, trade.BuyOrderId);
        Assert.Equal(1, trade.SellOrderId);

        Assert.Null(book.BestBid());
        Assert.Null(book.BestAsk());
    }
    [Fact]
    public void Sell_CrossingBuy_CreatesTrade()
    {
        var book = CreateBook();
        book.Add(CreateOrder(Side.Buy, 100.00m, 1));
        var trades = book.Add(CreateOrder(Side.Sell, 99.00m, 2));
        var trade = Assert.Single(trades);
        Assert.Equal(100.00m, trade.Price);
        Assert.Equal(5m, trade.Quantity);
        Assert.Equal(1, trade.BuyOrderId);
        Assert.Equal(2, trade.SellOrderId);
        Assert.Null(book.BestBid());
        Assert.Null(book.BestAsk());
    }
    [Fact]
    public void Buy_WalksTwoPriceLevels()
    {
        var book = CreateBook();
        book.Add(CreateOrder(Side.Sell, 100.00m, 1, 2m));
        var seller2 = CreateOrder(Side.Sell, 100.50m, 2, 10m);
        book.Add(seller2);
        var trades = book.Add(CreateOrder(Side.Buy, 100.50m, 3, 5m));
        Assert.Equal(2m, trades.Count);
        Assert.Equal(2m, trades[0].Quantity);
        Assert.Equal(3m, trades[1].Quantity);
        Assert.Equal(100.00m, trades[0].Price);
        Assert.Equal(100.50m, trades[1].Price);
        Assert.Equal(1, trades[0].SellOrderId);
        Assert.Equal(2, trades[1].SellOrderId);
        Assert.Equal(100.50m, book.BestAsk());
        Assert.Equal(7m, seller2.RemainingQuantity);
    }

    [Fact]
    public void Buy_PartialFill_RestsRemainder()
    {
        var book = CreateBook();
        book.Add(CreateOrder(Side.Sell, 99.00m, 1, 2m));
        var buyer = CreateOrder(Side.Buy, 100.00m, 2, 5m);
        var trades = book.Add(buyer);
        var trade = Assert.Single(trades);
        Assert.Equal(100.00m, book.BestBid());
        Assert.Null(book.BestAsk());
        Assert.Equal(99.00m, trade.Price);
        Assert.Equal(3m, buyer.RemainingQuantity);
        Assert.Equal(2m, trade.Quantity);
    }

    [Fact]
    public void SamePrice_FirstInFirstOut()
    {
        var book = CreateBook();
        var seller1 = CreateOrder(Side.Sell, 100.00m, 1);
        var seller2 = CreateOrder(Side.Sell, 100.00m, 2);
        var buyer = CreateOrder(Side.Buy, 100.00m, 3, 5m);
        book.Add(seller1);
        book.Add(seller2);
        var trades = book.Add(buyer);
        var trade = Assert.Single(trades);
        Assert.Equal(1, trade.SellOrderId);
        Assert.Equal(5m, seller2.RemainingQuantity);
        Assert.Equal(100.00m, book.BestAsk());
    }

    [Fact]
    public void Sell_HitsHighestBidFirst()
    {
        var book = CreateBook();
        var buyer1 = CreateOrder(Side.Buy, 95.00m, 1);
        var buyer2 = CreateOrder(Side.Buy, 100.00m, 2);
        var seller = CreateOrder(Side.Sell, 99.00m, 3);
        book.Add(buyer1);
        book.Add(buyer2);

        var trades = book.Add(seller);

        var trade = Assert.Single(trades);
        Assert.Equal(2, trade.BuyOrderId);
        Assert.Equal(100.00m, trade.Price);
        Assert.True(buyer2.IsFilled);
        Assert.Equal(5m, buyer1.RemainingQuantity);
        Assert.Equal(95.00m, book.BestBid());

    }

    [Fact]
    public void QuantityAt_SumsAllOrdersAtPrice()
    {
        var book = CreateBook();
        var buyer1 = CreateOrder(Side.Buy, 99.00m, 1, 3m);
        var buyer2 = CreateOrder(Side.Buy, 99.00m, 2, 4m);
        var buyer3 = CreateOrder(Side.Buy, 98.00m, 3, 10m);

        book.Add(buyer1);
        book.Add(buyer2);
        book.Add(buyer3);

        decimal buyAt99 = book.QuantityAt(Side.Buy, 99.00m);
        decimal sellAt99 = book.QuantityAt(Side.Sell, 99.00m);

        Assert.Equal(7m, buyAt99);
        Assert.Equal(0m, sellAt99);
    }
    [Fact]
    public void QuantityAt_CountsOnlyRemainingQuantity()
    {
        var book = CreateBook();
        var buyer1 = CreateOrder(Side.Buy, 99.00m, 1, 10m);
        var seller1 = CreateOrder(Side.Sell, 99.00m, 2, 4m);
        book.Add(buyer1);
        var trades = book.Add(seller1);
        var trade = Assert.Single(trades);
        Assert.Equal(6m, book.QuantityAt(Side.Buy, 99.00m));
    }

    [Fact]
    public void Add_PriceNotOnTick_Throws()
    {
        var book = CreateBook();
        var order = CreateOrder(Side.Buy, 99.005m);

        Assert.Throws<ArgumentException>(() => book.Add(order));
        Assert.Null(book.BestBid());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Add_NonPositivePrice_Throws(int price)
    {
        var book = CreateBook();
        Assert.Throws<ArgumentException>(() => book.Add(CreateOrder(Side.Buy, price)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Add_NonPositiveQuantity_Throws(int quantity)
    {
        var book = CreateBook();
        Assert.Throws<ArgumentException>(() => book.Add(CreateOrder(Side.Sell, 99.00m, 1, quantity)));
    }

    [Fact]
    public void Cancel_RestingOrder_RemovesItFromBook()
    {
        var book = CreateBook();
        var buyer = CreateOrder(Side.Buy, 99.00m, 1);
        book.Add(buyer);

        bool canceled = book.Cancel(1);
        Assert.True(canceled);
        Assert.Null(book.BestBid());
    }
    [Fact]
    public void Cancel_UnknownId_ReturnsFalse()
    {
        var book = CreateBook();
        bool canceled = book.Cancel(42);
        Assert.False(canceled);
    }
    [Fact]
    public void Cancel_MiddleOfQueue_KeepsTimePriority()
    {
        var book = CreateBook();
        var buyer = CreateOrder(Side.Buy, 99.00m, 1);
        var buyer2 = CreateOrder(Side.Buy, 99.00m, 2);
        var buyer3 = CreateOrder(Side.Buy, 99.00m, 3);
        book.Add(buyer);
        book.Add(buyer2);
        book.Add(buyer3);
        book.Cancel(2);
        var seller = CreateOrder(Side.Sell, 99.00m, 4);
        var trades = book.Add(seller);
        var trade = Assert.Single(trades);
        Assert.Equal(1, trade.BuyOrderId);

        Assert.Equal(5m, book.QuantityAt(Side.Buy, 99.00m));
        
    }
    [Fact]
    public void Add_DuplicateId_Throws()
    {
        var book = CreateBook();
        var buy1 = CreateOrder(Side.Buy, 99.00m, 1);
        var sell1 = CreateOrder(Side.Sell, 99.00m, 1);
        book.Add(buy1);
        Assert.Throws<ArgumentException>(() => book.Add(sell1));
        Assert.Equal(5m, book.QuantityAt(Side.Buy, 99.00m));
    }
}

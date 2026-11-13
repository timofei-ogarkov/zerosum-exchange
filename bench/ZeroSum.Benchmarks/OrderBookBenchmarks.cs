using BenchmarkDotNet.Attributes;
using ZeroSum.Engine;

namespace ZeroSum.Benchmarks;

public class OrderBookBenchmarks
{
    [Params("SortedDictionary", "Bitmap")]
    public string Book = "";

    private IOrderBook CreateBook()
    {
        return Book == "Bitmap" ? new BitmapOrderBook() : new SortedDictionaryOrderBook();
    }

    private static Order MakeOrder(long id, Side side, decimal price)
    {
        return new Order
        {
            Id = id,
            TraderId = "bench",
            OrderSide = side,
            Type = OrderType.Limit,
            Price = price,
            Quantity = 1m,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    [Benchmark]
    public int FillThenMatch()
    {
        var book = CreateBook();
        long id = 1;

        for (int i = 0; i < 500; i++)
        {
            book.Add(MakeOrder(id++, Side.Buy, 94.99m - (i % 100) * 0.01m));
            book.Add(MakeOrder(id++, Side.Sell, 105.00m + (i % 100) * 0.01m));
        }

        int trades = 0;
        for (int i = 0; i < 500; i++)
        {
            trades += book.Add(MakeOrder(id++, Side.Buy, 109.99m)).Count;
            trades += book.Add(MakeOrder(id++, Side.Sell, 90.00m)).Count;
        }

        return trades;
    }
}
namespace ZeroSum.Engine;

public class OrderBook
{
    private readonly SortedDictionary<decimal, Queue<Order>> _bids = new();
    private readonly SortedDictionary<decimal, Queue<Order>> _asks = new();
    public void Add(Order order)
    {
        if (order.Price is null)
        {
            throw new ArgumentException("Market orders cannot rest in the book", nameof(order));
        }
        decimal price = order.Price.Value;
        var side = order.OrderSide == Side.Buy ? _bids : _asks;
        if (!side.ContainsKey(price))
        {
            side.Add(price, new Queue<Order>());

        }
        side[price].Enqueue(order);
    }
    public decimal? BestBid()
    {
        if (_bids.Count == 0)
        {
            return null;
        }
        return _bids.Keys.Last();
    }
    public decimal? BestAsk()
    {
        if (_asks.Count == 0)
        {
            return null;
        }
        return _asks.Keys.First();
    }
}

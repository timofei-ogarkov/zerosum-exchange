namespace ZeroSum.Engine;

public class SortedDictionaryOrderBook : IOrderBook
{
    private readonly SortedDictionary<decimal, Queue<Order>> _bids = new();
    private readonly SortedDictionary<decimal, Queue<Order>> _asks = new();
    public List<Trade> Add(Order order)
    {
        if (order.Price is null)
        {
            throw new ArgumentException("Market orders cannot rest in the book", nameof(order));
        }
        decimal price = order.Price.Value;
        var trades = new List<Trade>();
        if(order.OrderSide == Side.Buy)
        {
        while (order.RemainingQuantity > 0 && _asks.Count > 0 && _asks.Keys.First()<= price)
            {
                var bestAsk = _asks.Keys.First();
                var queue = _asks[bestAsk];
                var resting = queue.Peek();
                decimal quantity = Math.Min(order.RemainingQuantity, resting.RemainingQuantity);
                order.Fill(quantity);
                resting.Fill(quantity);
                trades.Add(new Trade(order.Id, resting.Id, bestAsk, quantity));

                if (resting.IsFilled)
                {
                    queue.Dequeue();
                }

                if (queue.Count == 0)
                {
                    _asks.Remove(bestAsk);
                }

            }
        }
        else
        {
            while (order.RemainingQuantity > 0 && _bids.Count > 0 && _bids.Keys.Last()>= price)
            {
                var bestBid = _bids.Keys.First();
                var queue = _bids[bestBid];
                var resting = queue.Peek();
                decimal quantity = Math.Min(order.RemainingQuantity, resting.RemainingQuantity);
                order.Fill(quantity);
                resting.Fill(quantity);
                trades.Add(new Trade(resting.Id, order.Id, bestBid, quantity));

                if (resting.IsFilled)
                {
                    queue.Dequeue();
                }

                if (queue.Count == 0)
                {
                    _bids.Remove(bestBid);
                }

            }
        }
        if(order.RemainingQuantity > 0)
        {
        var side = order.OrderSide == Side.Buy ? _bids : _asks;
        if (!side.ContainsKey(price))
        {
            side.Add(price, new Queue<Order>());

        }
        side[price].Enqueue(order);
        }
        return trades;
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

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
        bool isBuy = order.OrderSide == Side.Buy;
        var opposite = isBuy ? _asks : _bids;

        while (order.RemainingQuantity > 0)
        {
            decimal? best = isBuy ? BestAsk() : BestBid();
            if (best is null)
            {
                break;
            }

            decimal bestPrice = best.Value;
            bool crosses = isBuy ? bestPrice <= price : bestPrice >= price;
            if (!crosses)
            {
                break;
            }

            var queue = opposite[bestPrice];
            var resting = queue.Peek();
            decimal quantity = Math.Min(order.RemainingQuantity, resting.RemainingQuantity);
            order.Fill(quantity);
            resting.Fill(quantity);

            var trade = isBuy
                ? new Trade(order.Id, resting.Id, bestPrice, quantity)
                : new Trade(resting.Id, order.Id, bestPrice, quantity);
            trades.Add(trade);

            if (resting.IsFilled)
            {
                queue.Dequeue();
            }

            if (queue.Count == 0)
            {
                opposite.Remove(bestPrice);
            }
        }

        if (order.RemainingQuantity > 0)
        {
            var own = isBuy ? _bids : _asks;
            if (!own.ContainsKey(price))
            {
                own.Add(price, new Queue<Order>());
            }
            own[price].Enqueue(order);
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

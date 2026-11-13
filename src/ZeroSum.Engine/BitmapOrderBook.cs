using System.Data.Common;
using System.Numerics;
namespace ZeroSum.Engine;

public class BitmapOrderBook : IOrderBook
{
    public const decimal LowPrice = 70.00m;
    public const int Levels = 4096;

    private readonly Dictionary<int, Queue<Order>> _bidLevels = new();
    private readonly ulong[] _bidWords = new ulong[64];
    private ulong _bidSummary;

    private readonly Dictionary<int, Queue<Order>> _askLevels = new();
    private readonly ulong[] _askWords = new ulong[64];
    private ulong _askSummary;

    private readonly Dictionary<long, Order> _orders = new();

    private static int ToPosition(decimal price) => (int)((price - LowPrice) / SortedDictionaryOrderBook.TickSize);
    private static decimal ToPrice(int position) => LowPrice + position * SortedDictionaryOrderBook.TickSize;

    public List<Trade> Add(Order order)
    {
        if (_orders.ContainsKey(order.Id))
        {
            throw new ArgumentException("Order id already exists", nameof(order));
        }
        if (order.Quantity <= 0)
        {
            throw new ArgumentException("Quantity must be positive", nameof(order));
        }

        bool isMarket = order.Type == OrderType.Market;
        if (!isMarket && order.Price is null)
        {
            throw new ArgumentException("Limit orders must have a price", nameof(order));
        }

        decimal price = order.Price ?? 0m;
        if (!isMarket && price <= 0)
        {
            throw new ArgumentException("Price must be positive", nameof(order));
        }
        if (!isMarket && price % SortedDictionaryOrderBook.TickSize != 0)
        {
            throw new ArgumentException("Price must be a multiple of the tick size", nameof(order));
        }
        int position = ToPosition(price);
        if (!isMarket && (position < 0 || position >= Levels))
        {
            throw new ArgumentException("Price is outside the book's price grid", nameof(order));
        }
        bool isBuy = order.OrderSide == Side.Buy;
        var trades = new List<Trade>();
        var oppositeLevels = isBuy ? _askLevels : _bidLevels;
        var oppositeSide = isBuy ? Side.Sell : Side.Buy;

        while (order.RemainingQuantity > 0)
        {
            decimal? best = isBuy ? BestAsk() : BestBid();
            if (best is null)
            {
                break;
            }

            decimal bestPrice = best.Value;
            bool crosses = isMarket || (isBuy ? bestPrice <= price : bestPrice >= price);
            if (!crosses)
            {
                break;
            }

            var queue = oppositeLevels[ToPosition(bestPrice)];
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
                _orders.Remove(resting.Id);
            }

            if (queue.Count == 0)
            {
                oppositeLevels.Remove(ToPosition(bestPrice));
                ClearBit(oppositeSide, ToPosition(bestPrice));
            }
        }
        if (order.RemainingQuantity > 0 && !isMarket)
        {
            var levels = isBuy ? _bidLevels : _askLevels;
            if (!levels.ContainsKey(position))
            {
                levels.Add(position, new Queue<Order>());
                SetBit(order.OrderSide, position);
            }
            levels[position].Enqueue(order);
            _orders.Add(order.Id, order);
        }

        return trades;
    }

    public decimal? BestBid()
    {
        if (_bidSummary == 0)
        {
            return null;
        }
        int wordIndex = 63 - BitOperations.LeadingZeroCount(_bidSummary);
        int bitIndex = 63 - BitOperations.LeadingZeroCount(_bidWords[wordIndex]);
        int position = wordIndex * 64 + bitIndex;
        return ToPrice(position);
    }

    public decimal? BestAsk()
    {
        if (_askSummary == 0)
        {
            return null;
        }
        int wordIndex = BitOperations.TrailingZeroCount(_askSummary);
        int bitIndex = BitOperations.TrailingZeroCount(_askWords[wordIndex]);
        int position = wordIndex * 64 + bitIndex;
        return ToPrice(position);
    }

    public decimal QuantityAt(Side side, decimal price)
    {
        int position = ToPosition(price);
        bool isBuy = side == Side.Buy;
        var levels = isBuy ? _bidLevels : _askLevels;
        if (!levels.ContainsKey(position))
        {
            return 0m;
        }
        decimal total = 0m;
        foreach (var order in levels[position])
        {
            total += order.RemainingQuantity;
        }
        return total;

    }

    public bool Cancel(long orderId)
    {
        if (!_orders.ContainsKey(orderId))
        {
            return false;
        }

        var order = _orders[orderId];
        bool isBuy = order.OrderSide == Side.Buy;
        var levels = isBuy ? _bidLevels : _askLevels;
        int position = ToPosition(order.Price!.Value);
        var oldQueue = levels[position];
        var newQueue = new Queue<Order>();

        foreach (var queued in oldQueue)
        {
            if (queued.Id != orderId)
            {
                newQueue.Enqueue(queued);
            }
        }
        if (newQueue.Count == 0)
        {
            levels.Remove(position);
            ClearBit(order.OrderSide, position);
        }
        else
        {
            levels[position] = newQueue;
        }
        _orders.Remove(orderId);
        return true;
    }

    private void SetBit(Side side, int position)
    {
        int wordIndex = position / 64;
        int bitIndex = position % 64;
        var words = side == Side.Buy ? _bidWords : _askWords;
        words[wordIndex] |= 1UL << bitIndex;
        if (side == Side.Buy)
        {
            _bidSummary |= 1UL << wordIndex;
        }
        else
        {
            _askSummary |= 1UL << wordIndex;
        }
    }

    private void ClearBit(Side side, int position)
    {
        int wordIndex = position / 64;
        int bitIndex = position % 64;
        var words = side == Side.Buy ? _bidWords : _askWords;
        words[wordIndex] &= ~(1UL << bitIndex);
        if (words[wordIndex] == 0)
        {
            if (side == Side.Buy)
            {
                _bidSummary &= ~(1UL << wordIndex);
            }
            else
            {
                _askSummary &= ~(1UL << wordIndex);
            }
        }
    }

}
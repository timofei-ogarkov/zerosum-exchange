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
        if (isMarket)
        {
            throw new ArgumentException("Market orders are not supported yet", nameof(order));
        }
        int position = ToPosition(price);
        if(position < 0 || position >= Levels)
        {
            throw new ArgumentException("Price is outside the book's price grid", nameof(order));
        }
        bool isBuy = order.OrderSide == Side.Buy;
        var levels = isBuy ? _bidLevels : _askLevels;
        if (!levels.ContainsKey(position))
        {
            levels.Add(position, new Queue<Order>()); 
            SetBit(order.OrderSide, position);
        }
        levels[position].Enqueue(order);
        _orders.Add(order.Id, order);
        return new List<Trade>();
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
        throw new NotImplementedException();
    }
    public bool Cancel(long orderId)
    {
        throw new NotImplementedException();
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
}
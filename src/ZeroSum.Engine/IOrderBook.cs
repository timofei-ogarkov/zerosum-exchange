namespace ZeroSum.Engine;

public interface IOrderBook
{
    List<Trade> Add(Order order);
    decimal? BestBid();
    decimal? BestAsk();
    decimal QuantityAt(Side side, decimal price);
    bool Cancel(long orderId);
}
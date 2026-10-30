namespace ZeroSum.Engine;

public interface IOrderBook
{
    List<Trade> Add(Order order);
    decimal? BestBid();
    decimal? BestAsk();
}
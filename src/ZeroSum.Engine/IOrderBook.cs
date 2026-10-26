namespace ZeroSum.Engine;

public interface IOrderBook
{
    void Add(Order order);
    decimal? BestBid();
    decimal? BestAsk();
}
namespace ZeroSum.Engine;

public class BitmapOrderBook : IOrderBook
{
    public List<Trade> Add(Order order)
    {
        throw new NotImplementedException();
    }
    public decimal? BestBid()
    {
        throw new NotImplementedException();
    }
    public decimal? BestAsk()
    {
        throw new NotImplementedException();
    }
    public decimal QuantityAt(Side side, decimal price)
    {
        throw new NotImplementedException();
    }
    public bool Cancel(long orderId)
    {
        throw new NotImplementedException();
    }
}
namespace ZeroSum.Engine;
public enum Side
{
    Buy,
    Sell
}
public enum OrderType
{
    Limit,
    Market
}
public class Order
{
    public required long Id { get; init; } 
    public required string TraderId { get; init; } 
    public required Side OrderSide { get; init; }
    public required OrderType Type { get; init; }
    public decimal? Price { get; init; }
    public required decimal Quantity { get; init; }
    public decimal FilledQuantity { get; private set; }
    public decimal RemainingQuantity => Quantity - FilledQuantity;
    public required DateTimeOffset Timestamp { get; init; }
}
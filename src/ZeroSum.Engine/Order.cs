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

    public bool IsFilled => RemainingQuantity == 0;

    public void Fill(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive");
        }

        if (quantity > RemainingQuantity)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), $"Cannot fill {quantity}, only {RemainingQuantity} remaining");
        }

        FilledQuantity += quantity;
    }
}
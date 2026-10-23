namespace ZeroSum.Engine.Tests;
public class OrderTests
{
            private static Order CreateOrder(decimal quantity = 5m)
    {
        return new Order
        {
            Id = 1,
            TraderId = "anna",
            OrderSide = Side.Buy,
            Type = OrderType.Limit,
            Price = 100.50m,
            Quantity = quantity,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
    [Fact]
    public void Fill_MoreThanRemaining_Throws()
    {
        var order = CreateOrder();
        Assert.Throws<ArgumentOutOfRangeException>(() => order.Fill(7m));
    }
    [Fact]
    public void Fill_IncreasesFilledQuantity()
    {
        var order = CreateOrder();
        order.Fill(3m);
        Assert.Equal(3m, order.FilledQuantity);
        Assert.Equal(2m, order.RemainingQuantity);
    }
    [Fact]
    public void Fill_WholeQuantity_MarksAsFilled()
    {
        var order = CreateOrder();
        order.Fill(5m);
        Assert.True(order.IsFilled);
    }
    [Fact]
    public void NewOrder_IsNotFilled()
    {
        var order = CreateOrder();
        Assert.Equal(0m, order.FilledQuantity);
        Assert.False(order.IsFilled);
    }
}
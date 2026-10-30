namespace ZeroSum.Engine;

public record Trade(long BuyOrderId, long SellOrderId, decimal Price, decimal Quantity);
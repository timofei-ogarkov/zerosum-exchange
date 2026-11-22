using ZeroSum.Engine;
namespace ZeroSum.Api;
public record OrderRequest(Side Side, OrderType Type, decimal? Price, decimal Quantity);


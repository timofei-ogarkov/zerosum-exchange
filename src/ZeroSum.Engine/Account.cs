namespace ZeroSum.Engine;
public enum Asset
{
    Usd,
    Gold
}
public class Account
{
    public string Id { get; }
    private readonly Dictionary<Asset, decimal> _available = new();
    private readonly Dictionary<Asset, decimal> _reserved = new();
    public Account(string id)
    {
        Id = id;
    }
    public decimal Available(Asset asset)
    {
        return _available.GetValueOrDefault(asset);
    }
    public decimal Reserved(Asset asset)
    {
        return _reserved.GetValueOrDefault(asset);
    }
    public void Deposit(Asset asset, decimal amount)
    {
        if(amount <= 0)
        {
            throw new ArgumentException("Amount must be positive");
        }
        _available[asset] = Available(asset) + amount;
    }
}
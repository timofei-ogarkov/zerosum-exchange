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
    public void Reserve(Asset asset, decimal amount)
    {
        if(amount <= 0)
        {
            throw new ArgumentException("Amount must be positive");
        }
        if(amount > Available(asset))
        {
            throw new InvalidOperationException("Insufficient funds");
        }
        _available[asset] = Available(asset) - amount;
        _reserved[asset] = Reserved(asset) + amount;
    }
    public void Release(Asset asset, decimal amount)
    {
        if(amount <= 0)
        {
            throw new ArgumentException("Amount must be positive");
        }
        if(amount > Reserved(asset))
        {
            throw new InvalidOperationException("Not enough reserved");
        }
        _reserved[asset] = Reserved(asset) - amount;
         _available[asset] = Available(asset) + amount;
    }
}
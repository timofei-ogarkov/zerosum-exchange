namespace ZeroSum.Engine.Tests;

public class AccountTests
{
    [Fact]
    public void NewAccount_HasZeroBalances()
    {
        var account = new Account("anna");
        Assert.Equal(0m, account.Available(Asset.Usd));
        Assert.Equal(0m, account.Reserved(Asset.Usd));
    }
    [Fact]
    public void Deposit_PositiveAmount_AddsToAvailable()
    {
        var account = new Account("anna");
        account.Deposit(Asset.Usd, 10000m);
        Assert.Equal(10000m, account.Available(Asset.Usd));
        Assert.Equal(0m, account.Available(Asset.Gold));
    }
    [Fact]
    public void Deposit_NonPositiveAmount_Throws()
    {
       var account = new Account("anna");
        Assert.Throws<ArgumentException>(() => account.Deposit(Asset.Usd, 0m));
    }
    [Fact]
    public void Reserve_WithinAvailable_MovesToReserved()
    {
        var account = new Account("anna");
        account.Deposit(Asset.Usd, 10000m);
        account.Reserve(Asset.Usd, 301.50m);

        Assert.Equal(9698.50m, account.Available(Asset.Usd));
        Assert.Equal(301.50m, account.Reserved(Asset.Usd));
    }
    [Fact]
    public void Reserve_MoreThanAvailable_ThrowsAndKeepsBalances()
    {
        var account = new Account("anna");
        account.Deposit(Asset.Usd, 200m);
        Assert.Throws<InvalidOperationException>(() => account.Reserve(Asset.Usd, 300m));
        Assert.Equal(200m, account.Available(Asset.Usd));
        Assert.Equal(0m, account.Reserved(Asset.Usd));
    }
    [Fact]
    public void Reserve_SecondOrderExceedsRemaining_Throws()
    {
        var account = new Account("anna");
        account.Deposit(Asset.Usd, 200m);
        account.Reserve(Asset.Usd, 150m);

        Assert.Throws<InvalidOperationException>(() => account.Reserve(Asset.Usd, 150m)); 
        Assert.Equal(50m, account.Available(Asset.Usd));
    }
    [Fact]
    public void Release_ReservedAmount_MovesBackToAvailable()
    {
        var account = new Account("anna");
        account.Deposit(Asset.Usd, 10000m);
        account.Reserve(Asset.Usd, 301.50m);
        account.Release(Asset.Usd, 301.50m);

        Assert.Equal(10000m, account.Available(Asset.Usd));
    }
    [Fact]
    public void Release_MoreThanReserved_Throws()
    {
        var account = new Account("anna");
        account.Deposit(Asset.Usd, 10000m);
        account.Reserve(Asset.Usd, 100m);
        Assert.Throws<InvalidOperationException>(() => account.Release(Asset.Usd, 150m));
    }
}
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
}
using ActorModel.Domain.Banking;
using FluentAssertions;

namespace ActorModel.Domain.Tests;

public class AccountShould
{
    [Fact]
    public void IncreaseFundsWhenExchangeRateIsDefined()
    {
        var account = new Account(new AccountReference("a-reference"), new Money(123, new CurrencyCode("EUR")));
        
        var result = account.IncreaseFunds(new Money(111, new CurrencyCode("EUR")));
        
        result.Code.Should().Be(TransferResultCode.SUCCESS);
        account.Balance.Amount.Should().Be(123 + 111);
    }

    [Fact]
    public void NotDecreaseFundsWhenCurrencyDoesNotMatchTheRequested()
    {
        var account = new Account(new AccountReference("a-reference"), new Money(123, new CurrencyCode("EUR")));

        var result = account.DecreaseFunds(new Money(111, new CurrencyCode("GBP")));

        result.Code.Should().Be(TransferResultCode.INVALID_SOURCE_CURRENCY);
        account.Balance.Amount.Should().Be(123);
    }

    [Fact]
    public void NotDecreaseFundsWhenInsufficientBalance()
    {
        var account = new Account(new AccountReference("a-reference"), new Money(123, new CurrencyCode("EUR")));

        var result = account.DecreaseFunds(new Money(140, new CurrencyCode("EUR")));

        result.Code.Should().Be(TransferResultCode.INSUFFICIENT_FUNDS);
        account.Balance.Amount.Should().Be(123);
    }

    [Fact]
    public void DecreaseFundsWhenCurrencyMatchesAndHasEnoughMoney()
    {
        var account = new Account(new AccountReference("a-reference"), new Money(123, new CurrencyCode("EUR")));

        var result = account.DecreaseFunds(new Money(111, new CurrencyCode("EUR")));

        result.Code.Should().Be(TransferResultCode.SUCCESS);
        account.Balance.Amount.Should().Be(123 - 111);
    }
}

using ActorModel.Domain.Banking;
using ActorModel.Persistence.Banking;
using ActorModel.Persistence.Banking.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ActorModel.Persistence.Tests;

public class ExchangeRepositoryShould
{
    private const string EuroCurrencyCode = "EUR";
    private const string BritishPoundCurrencyCode = "GBP";
    private readonly IExchangeRepository exchangeRepository = new ExchangeRepository(GetDbContext());

    [Fact]
    public void ReturnNullWhenNoExchangeRateIsFound()
    {
        var rate = exchangeRepository.GetRate(new CurrencyCode(EuroCurrencyCode), new CurrencyCode(BritishPoundCurrencyCode));
        rate.Should().BeNull();
    }

    [Fact]
    public void ReturnTheRateWhenDirectConversionExists()
    {
        GivenAnExchangeRateBetween(EuroCurrencyCode, BritishPoundCurrencyCode, 2);
        var rate = exchangeRepository.GetRate(new CurrencyCode(EuroCurrencyCode), new CurrencyCode(BritishPoundCurrencyCode));
        rate.Should().Be(2);
    }

    private static void GivenAnExchangeRateBetween(string sourceCurrencyCode, string targetCurrencyCode, decimal rate)
    {
        using var context = GetDbContext();
        context.ExchangeRates.Add(new PersistentExchangeRate
        {
            Rate = rate,
            SourceCurrency = sourceCurrencyCode,
            TargetCurrency = targetCurrencyCode
        });
        context.SaveChanges();
    }

    private static BankingContext GetDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<BankingContext>()
            .UseInMemoryDatabase("banking-exchange-tests")
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        return new BankingContext(optionsBuilder.Options);

    }
}
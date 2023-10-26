using ActorModel.Domain.Banking;

namespace ActorModel.Persistence.Banking;

public class ExchangeRepository: IExchangeRepository
{
    private readonly BankingContext context;

    public ExchangeRepository(BankingContext context)
    {
        this.context = context;
    }

    public decimal? GetRate(CurrencyCode source, CurrencyCode target)
    {
        if (source == target) return 1;
        var exchangeRate = context.ExchangeRates
            .SingleOrDefault(p => p.SourceCurrency == source.Iso3LetterCode && p.TargetCurrency == target.Iso3LetterCode);
        return exchangeRate?.Rate;
    }
}
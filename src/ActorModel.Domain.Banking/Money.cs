namespace ActorModel.Domain.Banking;

public record Money(decimal Amount, CurrencyCode Currency)
{
    public Money Exchange(decimal rate, CurrencyCode currency)
    {
        return new Money(rate * Amount, currency);
    }
};
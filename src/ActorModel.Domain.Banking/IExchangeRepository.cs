namespace ActorModel.Domain.Banking;

public interface IExchangeRepository
{
    decimal? GetRate(CurrencyCode source, CurrencyCode target);
}
using ActorModel.Domain.Banking;

namespace ActorModel.Application;

public record IncreaseFunds(Money Amount);

public record DecreaseFunds(Money Amount);

public record ExchangeRateRequest(CurrencyCode SourceCurrency, CurrencyCode TargetCurrency);
namespace ActorModel.Domain.Banking;

public class Account
{
    public AccountReference Reference { get; }
    public Money Balance { get; private set; }

    public Account(AccountReference reference, Money balance)
    {
        Reference = reference;
        Balance = balance;
    }

    public TransferResponse IncreaseFunds(Money funds)
    {
        Balance = Balance with { Amount = Balance.Amount + funds.Amount };

        return new TransferResponse(TransferResultCode.SUCCESS, funds);
    }

    public TransferResponse DecreaseFunds(Money funds)
    {
        if (Balance.Currency != funds.Currency)
            return new TransferResponse(TransferResultCode.INVALID_SOURCE_CURRENCY);
        
        if (Balance.Amount < funds.Amount)
            return new TransferResponse(TransferResultCode.INSUFFICIENT_FUNDS);
        
        Balance = Balance with { Amount = Balance.Amount - funds.Amount };

        return new TransferResponse(TransferResultCode.SUCCESS, funds);
    }
}
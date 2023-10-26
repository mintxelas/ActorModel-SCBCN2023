namespace ActorModel.Domain.Banking;

public class TransferService
{
    private readonly IAccountsRepository accounts;
    private readonly IExchangeRepository exchange;

    public TransferService(IAccountsRepository accounts, IExchangeRepository exchange)
    {
        this.accounts = accounts;
        this.exchange = exchange;
    }

    public TransferResponse Execute(TransferRequest transferRequest)
    {
        var sourceAccount = accounts.ByReference(transferRequest.SourceAccount);
        var decreaseFundsResult = sourceAccount.DecreaseFunds(transferRequest.Amount);
        if (decreaseFundsResult.Code != TransferResultCode.SUCCESS)
            return decreaseFundsResult;

        var targetAccount = accounts.ByReference(transferRequest.TargetAccount);
        var exchangeRate = exchange.GetRate(transferRequest.Amount.Currency, targetAccount.Balance.Currency);

        if (exchangeRate is null)
            return new TransferResponse(TransferResultCode.NO_EXCHANGE_RATE);

        var exchangedFunds = transferRequest.Amount.Exchange(exchangeRate.Value, targetAccount.Balance.Currency);

        var increaseFundsResult = targetAccount.IncreaseFunds(exchangedFunds);
        if (increaseFundsResult.Code == TransferResultCode.SUCCESS)
            accounts.UpdateBalance(sourceAccount, targetAccount);

        return increaseFundsResult;
    }
}
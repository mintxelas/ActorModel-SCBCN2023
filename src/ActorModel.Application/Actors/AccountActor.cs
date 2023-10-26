using System.Data;
using ActorModel.Domain.Banking;
using ActorModel.Persistence.Banking;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Monitoring;
using Microsoft.EntityFrameworkCore;

namespace ActorModel.Application.Actors;

public class AccountActor : ReceiveActor
{
    private BankingContext? context;
    public AccountActor()
    {
        Context.IncrementActorCreated();

        var account = RetrieveAccount();

        ReceiveAsync<string>(_ =>
        {
            Context.IncrementMessagesReceived();
            Sender.Tell(account);
            return Task.CompletedTask;
        });

        ReceiveAsync<IncreaseFunds>(async request =>
        {
            Context.IncrementMessagesReceived();
            var exchangeRate = await Context.ActorSelection($"{Self.Path.Parent.Parent}/EXCHANGE_RATES")
                .Ask<decimal?>(new ExchangeRateRequest(request.Amount.Currency,account.Balance.Currency));
            if (exchangeRate is null)
            {
                Sender.Tell(new TransferResponse(TransferResultCode.NO_EXCHANGE_RATE));
                return;
            }

            var exchangedFunds = request.Amount.Exchange(exchangeRate.Value, account.Balance.Currency);

            var result = account.IncreaseFunds(request.Amount);
            if (result.Code == TransferResultCode.SUCCESS) 
                UpdateBalance(account.Balance.Amount);
            Sender.Tell(result);
        });

        ReceiveAsync<DecreaseFunds>(request =>
        {
            Context.IncrementMessagesReceived();
            var result = account.DecreaseFunds(request.Amount);
            if (result.Code == TransferResultCode.SUCCESS) 
                UpdateBalance(account.Balance.Amount);
            Sender.Tell(result);
            return Task.CompletedTask;
        });

        Receive<Stop>(_ =>
        {
            Context.IncrementActorStopped();
        });
    }

    private Account RetrieveAccount()
    {
        EnsureContext();
        var record = context!.Accounts.AsNoTracking().Single(p => p.Reference == Self.Path.Name);
        return new Account(new AccountReference(Self.Path.Name), new Money(record.Balance, new CurrencyCode(record.CurrencyCode)));
    }

    private void UpdateBalance(decimal balance)
    {
        EnsureContext();
        var record = context!.Accounts.Single(p => p.Reference == Self.Path.Name);
        record.Balance = balance;
        context.SaveChanges();
    }

    private void EnsureContext()
    {
        if (context?.Database.GetDbConnection().State != ConnectionState.Open) context = BankingContext.CreateInstance();
    }
}
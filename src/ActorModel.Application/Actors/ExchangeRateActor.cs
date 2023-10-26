using ActorModel.Persistence.Banking;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Monitoring;
using Microsoft.EntityFrameworkCore;

namespace ActorModel.Application.Actors;

public class ExchangeRateActor: ReceiveActor
{
    public ExchangeRateActor()
    {
        Context.IncrementActorCreated();

        var rate = RetrieveRate();

        ReceiveAsync<string>(_ =>
        {
            Context.IncrementMessagesReceived();
            Sender.Tell(rate);
            return Task.CompletedTask;
        });

        Receive<Stop>(_ =>
        {
            Context.IncrementActorStopped();
        });
    }

    private decimal? RetrieveRate()
    {
        using var context = BankingContext.CreateInstance();
        var parts = Self.Path.Name.Split('-');
        if (parts[0] == parts[1]) return 1;
        var exchangeRate = context.ExchangeRates.AsNoTracking()
            .SingleOrDefault(p => p.SourceCurrency == parts[0] && p.TargetCurrency == parts[1]);
        return exchangeRate?.Rate;
    }
}
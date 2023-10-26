using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Monitoring;

namespace ActorModel.Application.Actors;

public class ExchangeRatesActor: ReceiveActor
{
    public ExchangeRatesActor()
    {
        Context.IncrementActorCreated();

        ReceiveAsync<ExchangeRateRequest>(async request =>
        {
            Context.IncrementMessagesReceived();
            var name = $"{request.SourceCurrency.Iso3LetterCode}-{request.TargetCurrency.Iso3LetterCode}";
            var actorRef = Context.Child(name);
            if (actorRef is Nobody)
                actorRef = Context.ActorOf<ExchangeRateActor>(name);

            var rate = await actorRef.Ask<decimal?>("get");
            Sender.Tell(rate);
        });

        Receive<Stop>(_ =>
        {
            Context.IncrementActorStopped();
        });
    }
}
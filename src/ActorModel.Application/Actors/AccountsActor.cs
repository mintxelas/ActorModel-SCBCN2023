using ActorModel.Domain.Banking;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Monitoring;

namespace ActorModel.Application.Actors;

public class AccountsActor: ReceiveActor
{
    public AccountsActor()
    {
        Context.IncrementActorCreated();

        ReceiveAsync<AccountReference>(reference =>
        {
            Context.IncrementMessagesReceived();
            var actorRef = Context.Child(reference.Value);
            if (actorRef is Nobody)
                actorRef = Context.ActorOf<AccountActor>(reference.Value);
            
            Sender.Tell(actorRef);
            return Task.CompletedTask;
        });

        Receive<Stop>(_ =>
        {
            Context.IncrementActorStopped();
        });
    }
}
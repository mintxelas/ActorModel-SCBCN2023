using ActorModel.Domain.Banking;
using ActorModel.Persistence.Banking;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Monitoring;

namespace ActorModel.Application.Actors;

public class TransferServiceActor : ReceiveActor
{
    private int ok;
    private int ko;
    private int starts;
    private int completes;

    public TransferServiceActor()
    {
        Context.IncrementActorCreated();

        ReceiveAsync<TransferRequest>(transferRequest =>
        {
            Context.IncrementMessagesReceived(); 
            var concreteTransferActor = Context.Child(transferRequest.SourceAccount.Value);
            if (concreteTransferActor is Nobody)
                concreteTransferActor = Context.ActorOf<TransferActor>(transferRequest.SourceAccount.Value);

            starts += 1;
            
            concreteTransferActor.Tell(transferRequest, Sender);
            return Task.CompletedTask;
        });

        ReceiveAsync<TransferResponse>(transferResult =>
        {
            completes += 1;
            Context.IncrementMessagesReceived();
            if (transferResult.Code.IsSuccess) ok += 1;
            else ko += 1;

            PrometheusMetrics.TotalTransfersProcessed.Inc();

            return Task.CompletedTask;
        });

        ReceiveAsync<string>(command =>
        {
            Context.IncrementMessagesReceived();
            if (command == "summary") 
                Sender.Tell($"OK: {ok}, KO: {ko}, TOTAL: {completes}, IN PROCESS: {starts - completes}");

            if (command == "pending")
                Sender.Tell(starts - completes);

            if (command == "total")
                Sender.Tell(completes);
            
            return Task.CompletedTask;
        });

        Receive<Stop>(_ =>
        {
            Context.IncrementActorStopped();
        });
    }      
}
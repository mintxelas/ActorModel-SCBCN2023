using System.Diagnostics;
using ActorModel.Domain.Banking;
using ActorModel.Persistence.Banking;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Monitoring;
using Prometheus;

namespace ActorModel.Application.Actors;

public class TransferActor: ReceiveActor
{
    public TransferActor()
    {
        Context.IncrementActorCreated();

        ReceiveAsync<TransferRequest>(async transferRequest =>
        {
            Context.IncrementMessagesReceived();
            var transferResponse = await ExecuteTransfer(transferRequest);
            Sender.Tell(transferResponse);
            Context.Parent.Tell(transferResponse);
        });

        Receive<Stop>(_ =>
        {
            Context.IncrementActorStopped();
        });
    }

    private async Task<TransferResponse> ExecuteTransfer(TransferRequest transferRequest)
    {
        using (PrometheusMetrics.TransferTime.NewTimer())
        using (PrometheusMetrics.TransfersInProcess.TrackInProgress())
        {
            var sourceAccountActorRef = await Context.ActorSelection($"{Self.Path.Parent.Parent}/ACCOUNTS")
                .Ask<IActorRef>(transferRequest.SourceAccount);
            var sourceTransferResult =
                await sourceAccountActorRef.Ask<TransferResponse>(new DecreaseFunds(transferRequest.Amount));
            if (sourceTransferResult.Code != TransferResultCode.SUCCESS) return sourceTransferResult;

            var targetAccountActorRef = await Context.ActorSelection($"{Self.Path.Parent.Parent}/ACCOUNTS")
                .Ask<IActorRef>(transferRequest.TargetAccount);
            var targetTransferResult =
                await targetAccountActorRef.Ask<TransferResponse>(new IncreaseFunds(transferRequest.Amount));
            if (targetTransferResult.Code != TransferResultCode.SUCCESS)
            {
                var rollbackResponse =
                    await sourceAccountActorRef.Ask<TransferResponse>(new IncreaseFunds(transferRequest.Amount));
                if (rollbackResponse.Code != TransferResultCode.SUCCESS)
                {
                    Console.WriteLine($"Rollback failed for {transferRequest}");
                    throw new InvalidOperationException($"Rollback failed for {transferRequest}");
                }
            }
            
            return targetTransferResult;
        }
    }
}
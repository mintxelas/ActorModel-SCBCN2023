using ActorModel.Application.Actors;
using ActorModel.Domain.Banking;
using ActorModel.Persistence.Banking;
using Akka.Actor;
using Akka.Monitoring.Prometheus;
using Akka.Monitoring;
using Prometheus;
using System.Diagnostics;

namespace ActorModel.Application;

public class Program
{
    private const long N = 30_000;
    private const long Step = 100;

    public static async Task<int> Main(string[] args)
    {
        BankingContext.SeedDatabase();
        SetupPrometheusExporter();

        Console.WriteLine("Starting actorSystem...");
        var system = ActorSystem.Create("banking");
        _ = ActorMonitoringExtension.RegisterMonitor(system, new ActorPrometheusMonitor(system));

        var sw = Stopwatch.StartNew();
        var transferServiceActor = CreateActors(system);
        
        await LaunchTransfersInBatches(transferServiceActor);

        Console.WriteLine("Work finished.");
        sw.Stop();

        var summary = await transferServiceActor.Ask<string>("summary");
        Console.WriteLine(summary);

        Console.WriteLine($"Total Time: {sw.ElapsedMilliseconds} ms ({(sw.ElapsedMilliseconds / (2 * N)):F1} ms/transfer)");

        Console.WriteLine("Terminating actorSystem...");
        await system.Terminate();

        Console.WriteLine("done.");
        Console.ReadLine();
        return 0;
    }

    private static async Task LaunchTransfersInBatches(IActorRef transferServiceActor)
    {
        var publishedTransfersCounter = 0L;
        await using (CreateReportingTimer(transferServiceActor))
        {
            while (publishedTransfersCounter < N)
            {
                await DoTransfers(transferServiceActor, publishedTransfersCounter);
                publishedTransfersCounter += Step;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            while ((await transferServiceActor.Ask<int>("total")) < 2 * N) Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }

    private static void SetupPrometheusExporter()
    {
        var metricServer = new MetricServer(port: 1235);
        metricServer.Start();
    }

    private static Timer CreateReportingTimer(IActorRef transferServiceActor)
    {
        return new Timer(_ => Console.WriteLine(transferServiceActor.Ask<string>("summary").Result),
            null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }

    private static async Task DoTransfers(IActorRef transferServiceActor,long publishedTransfersCounter)
    {
        var sw = Stopwatch.StartNew();
        var random = new Random();
        Parallel.For(publishedTransfersCounter, publishedTransfersCounter + Step, new ParallelOptions
        {
            CancellationToken = CancellationToken.None,
            MaxDegreeOfParallelism = 8,
            TaskScheduler = TaskScheduler.Default
        }, async _ =>
        {
            var sourceNumber = random.Next(1, 501);
            var sourceAccountName = $"ACCOUNT_{sourceNumber}";
            var sourceAccountReference = new AccountReference(sourceAccountName);

            var targetNumber = random.Next(501, 1001);
            var targetAccountName = $"ACCOUNT_{targetNumber}";
            var targetAccountReference = new AccountReference(targetAccountName);

            var transferRequest = new TransferRequest(sourceAccountReference, targetAccountReference,
                new Money(random.Next(10, 100), new CurrencyCode("EUR")));
            var t1 = await transferServiceActor.Ask<TransferResponse>(transferRequest);

            var backTransferRequest = new TransferRequest(targetAccountReference, sourceAccountReference,
                new Money(random.Next(10, 100), new CurrencyCode("GBP")));
            var t2 = await transferServiceActor.Ask<TransferResponse>(backTransferRequest);
        });

        Console.WriteLine($"{2 * Step} Actors created in {sw.ElapsedMilliseconds} ms, waiting for them to finish work.");
    }
    
    private static IActorRef CreateActors(ActorSystem actorSystem)
    {
        _ = actorSystem.ActorOf<ExchangeRatesActor>("EXCHANGE_RATES");
        _ = actorSystem.ActorOf<AccountsActor>("ACCOUNTS");
        return actorSystem.ActorOf<TransferServiceActor>("TRANSFER_SERVICE");
    }
}
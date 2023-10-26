using ActorModel.Domain.Banking;
using ActorModel.Persistence.Banking;
using Locking.Application.Tasks;
using Prometheus;
using System.Diagnostics;

namespace Locking.Application;

public class Program
{
    private const long N = 30_000;
    private const int Step = 100;


    private static async Task<int> Main(string[] args)
    {
        BankingContext.SeedDatabase();
        SetupPrometheusExporter();

        var sw = Stopwatch.StartNew();
        Console.WriteLine("Starting task creation...");
        
        await LaunchTransfersInBatches();
        
        sw.Stop();

        Console.WriteLine(TransferTask.Summary);
        Console.WriteLine($"Total Time: {sw.ElapsedMilliseconds} ms ({(sw.ElapsedMilliseconds / (2 * N)):F1} ms/transfer)");
        Console.WriteLine("done.");
        Console.ReadLine();
        return 0;
    }

    private static async Task LaunchTransfersInBatches()
    {
        var publishedTransfersCounter = 0L;
        await using (CreateReportingTimer())
        {
            while (publishedTransfersCounter < N)
            {
                DoTransfers(publishedTransfersCounter);
                publishedTransfersCounter += Step;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            while (TransferTask.Total < 2 * N) Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }

    private static void SetupPrometheusExporter()
    {
        var metricServer = new MetricServer(port: 1234);
        metricServer.Start();
    }

    private static Timer CreateReportingTimer()
    {
        return new Timer(_ => Console.WriteLine(TransferTask.Summary), null, TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(10));
    }

    private static void DoTransfers(long publishedTransfersCounter)
    {
        var sw = Stopwatch.StartNew();
        var random = new Random();
        Parallel.For(publishedTransfersCounter, publishedTransfersCounter + Step, new ParallelOptions
        {
            CancellationToken = CancellationToken.None,
            MaxDegreeOfParallelism = 8,
            TaskScheduler = TaskScheduler.Default
        }, _ =>
        {
            var sourceNumber = random.Next(1, 501);
            var sourceAccountName = $"ACCOUNT_{sourceNumber}";
            var sourceAccountReference = new AccountReference(sourceAccountName);

            var targetNumber = random.Next(501, 1001);
            var targetAccountName = $"ACCOUNT_{targetNumber}";
            var targetAccountReference = new AccountReference(targetAccountName);

            var transferRequest = new TransferRequest(sourceAccountReference, targetAccountReference,
                new Money(random.Next(10, 100), new CurrencyCode("EUR")));
            var transfer = new TransferTask(transferRequest);
            transfer.Start();

            var backTransferRequest = new TransferRequest(targetAccountReference, sourceAccountReference,
                new Money(random.Next(10, 100), new CurrencyCode("GBP")));
            var backTransfer = new TransferTask(backTransferRequest);
            backTransfer.Start();
        });

        Console.WriteLine($"{2 * Step} Tasks created in {sw.ElapsedMilliseconds} ms, waiting for them to finish work.");
    }
}
using System.Collections.Concurrent;
using ActorModel.Domain.Banking;
using ActorModel.Persistence.Banking;
using Prometheus;

namespace Locking.Application.Tasks;

public class TransferTask: Task<TransferResponse>
{
    private static readonly DbLock CounterLock = BankingContext.GetDbLock("counters");
    private static readonly ConcurrentDictionary<string, DbLock> AccountLocks = new();
    private static readonly ConcurrentDictionary<string, TransferService> TransferServiceInstances = new();
    private static int ok;
    private static int ko;

    public static string Summary => $"OK: {ok}, KO: {ko}, TOTAL: {ok + ko}";

    public static long Total => ok + ko;

    public TransferTask(TransferRequest request) : this(Execute, request) { }

    private TransferTask(Func<object?, TransferResponse> function, TransferRequest request) : base(function, request) { }

    private static TransferResponse Execute(object? request) => Process((TransferRequest)request!);

    private static TransferResponse Process(TransferRequest transferRequest)
    {
        using (PrometheusMetrics.TransferTime.NewTimer())
        using (PrometheusMetrics.TransfersInProcess.TrackInProgress())
        {
            var accountLock = AccountLocks.GetOrAdd(transferRequest.SourceAccount.Value, BankingContext.GetDbLock);
            var transferService = TransferServiceInstances.GetOrAdd(transferRequest.SourceAccount.Value, key =>
            {
                var accountsRepository = new AccountsRepository(BankingContext.CreateInstance());
                var exchangeRepository = new ExchangeRepository(BankingContext.CreateInstance());
                return new TransferService(accountsRepository, exchangeRepository);
            });

            TransferResponse? transferResult = null;
            accountLock.Lock(() => { transferResult = transferService.Execute(transferRequest); });

            CounterLock.Lock(() =>
            {
                if (transferResult!.Code.IsSuccess)
                {
                    ++ok;
                }

                if (!transferResult.Code.IsSuccess)
                {
                    ++ko;
                }
            });

            PrometheusMetrics.TotalTransfersProcessed.Inc();
            return transferResult!;
        }
    }
}
using Prometheus;

namespace ActorModel.Persistence.Banking;

public static class PrometheusMetrics
{
    public static Counter TotalTransfersProcessed = Metrics.CreateCounter("transfers_processed_total", "Total number of records processed.");

    public static Gauge TransfersInProcess = Metrics.CreateGauge("transfers_in_process", "Number of transfers currently being processed");

    public static Histogram TransferTime = Metrics.CreateHistogram("transfer_time", "Time taken per transaction");
}
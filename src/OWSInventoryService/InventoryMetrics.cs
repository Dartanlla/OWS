using System.Diagnostics.Metrics;

namespace OWSInventoryService;

internal static class InventoryMetrics
{
    public const string MeterName = "OWS.Inventory";
    private static readonly Meter Meter = new(MeterName);

    public static readonly Histogram<double> Acceptance = Meter.CreateHistogram<double>("ows.inventory.acceptance", "ms");
    public static readonly Histogram<double> Confirmation = Meter.CreateHistogram<double>("ows.inventory.confirmation", "ms");
    public static readonly Histogram<double> FlushDuration = Meter.CreateHistogram<double>("ows.inventory.flush", "ms");
    public static readonly Histogram<double> PersistenceLag = Meter.CreateHistogram<double>("ows.inventory.persistence.lag", "ms");
    public static readonly Counter<long> Persisted = Meter.CreateCounter<long>("ows.inventory.persisted", "{command}");
    public static readonly Counter<long> Quarantined = Meter.CreateCounter<long>("ows.inventory.bucket.quarantined", "{bucket}");
}

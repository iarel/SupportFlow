using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SupportFlow.BuildingBlocks.Outbox;

/// <summary>
/// Traces and metrics of outbox delivery (containers.md, Observability). Exported by the hosts' OpenTelemetry
/// setup, which listens to <c>SupportFlow.*</c>.
/// </summary>
internal static class OutboxTelemetry
{
    public const string Name = "SupportFlow.Outbox";

    public const string EventTypeTag = "supportflow.event.type";

    public const string OutboxTag = "supportflow.outbox";

    public static readonly ActivitySource ActivitySource = new(Name);

    private static readonly Meter _meter = new(Name);

    public static readonly Histogram<double> DeliveryDuration = _meter.CreateHistogram<double>(
        "supportflow.outbox.delivery.duration",
        unit: "s",
        description: "Time to deliver an integration event to all its handlers.");

    public static readonly Counter<long> DeliveryFailures = _meter.CreateCounter<long>(
        "supportflow.outbox.delivery.failures",
        description: "Failed delivery attempts, including the ones that parked the event.");

    public static readonly Counter<long> Parked = _meter.CreateCounter<long>(
        "supportflow.outbox.parked",
        description: "Events parked after exhausting their attempts.");

    public static readonly Gauge<double> Lag = _meter.CreateGauge<double>(
        "supportflow.outbox.lag",
        unit: "s",
        description: "Age of the oldest undelivered event; 0 when the outbox is empty.");

    public static readonly Gauge<long> ParkedNow = _meter.CreateGauge<long>(
        "supportflow.outbox.parked.current",
        description: "Parked events waiting for inspection or replay.");
}

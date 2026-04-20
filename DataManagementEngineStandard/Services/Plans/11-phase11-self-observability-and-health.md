# Phase 11 — Self-Observability & Health

## Objective

Make the logging/audit subsystem **observable about itself** so operators can see queue pressure, dropped events, sink failures, retention sweeps, and storage-budget breaches without attaching a debugger.

## Dependencies

- Phase 02 pipeline.
- Phase 04 budget enforcer.
- Phase 08 chain (for chain-health surfaces).

## Scope

- **In**: In-process metrics, sink health probes, periodic snapshot, self-events.
- **Out**: Pushing metrics over the network (covered by the OTel exporter in Phase 13/v2).

## Target files

```
Services/Telemetry/Diagnostics/
  PipelineMetrics.cs                  // partial: .Counters, .Gauges, .Snapshot
  PipelineMetrics.Counters.cs
  PipelineMetrics.Gauges.cs
  PipelineMetrics.Snapshot.cs
  ISinkHealthProbe.cs
  SinkHealth.cs
  HealthAggregator.cs
  PeriodicMetricsSnapshotHostedService.cs
  SelfEventCategory.cs                // "BeepTelemetry.Self"
```

## Design notes

### Counters & gauges

Counters (monotonic):
- `enqueued_total` per kind (log/audit).
- `dropped_total` per stage (sampler / deduper / rate-limiter / queue-full).
- `sink_errors_total` per `sinkName`.
- `sweeper_deletes_total`, `sweeper_compress_total`.
- `chain_signed_total`, `chain_verified_total`, `chain_divergence_total`.

Gauges (point-in-time):
- `queue_depth_current`, `queue_capacity`.
- `disk_total_bytes` per sink directory.
- `disk_budget_bytes`, `disk_budget_used_pct`.
- `last_flush_latency_ms`.

All values are read-only snapshots fetched via `PipelineMetrics.Snapshot()` — returns a small POCO so callers (UI, hosted service, OTel exporter) can serialize.

### Sink health

```csharp
public interface ISinkHealthProbe
{
    string Name { get; }
    SinkHealth Probe();
}

public sealed class SinkHealth
{
    public bool      IsHealthy   { get; init; }
    public DateTime? LastSuccessUtc { get; init; }
    public DateTime? LastErrorUtc   { get; init; }
    public string    LastError      { get; init; }
    public int       ConsecutiveFailures { get; init; }
}
```

Sinks update their own probe inside `WriteBatchAsync`. `HealthAggregator` rolls them up into a single `IsHealthy` for the pipeline.

### Self events

When something interesting happens internally (drop spike, sink unhealthy, budget breach, chain divergence), the pipeline emits a normal `IBeepLog` envelope under category `BeepTelemetry.Self`. These are the **operator's primary signal**.

Self events also have backpressure protection: deduped to one per minute per (event-type, sink) pair so we never amplify a spiral.

### Periodic snapshot

`PeriodicMetricsSnapshotHostedService` (opt-in, default off) writes a `metrics.txt` (or `.json`) every `SnapshotInterval` to the same directory as the sinks. Useful in environments without a metrics backend; otherwise the OTel exporter (Phase 13) handles this.

### Operator surface

```csharp
opt.EnableSelfMetrics      = true;
opt.SnapshotIntervalMin    = 5;
opt.OnSelfEvent           += (env) => Console.Error.WriteLine(env.Message);
```

## Implementation steps

1. Add `PipelineMetrics` partials with thread-safe counters (`Interlocked`) and snapshot record.
2. Add `ISinkHealthProbe` + `SinkHealth` POCO; default impl per sink.
3. Add `HealthAggregator`.
4. Pipe drop counters from samplers / deduper / rate-limiter / queue (Phase 07) into `PipelineMetrics`.
5. Pipe budget enforcer events (Phase 04) into self-events.
6. Pipe chain divergence events (Phase 08) into self-events.
7. Add the periodic snapshot hosted service.
8. Tests: simulate drops, sink failure, budget breach → verify counters and self events fire and dedupe.

## TODO checklist

- [x] P11-01 `PipelineMetrics.{Core,Counters,Gauges,Snapshot}.cs` (with `MetricsSnapshot` POCO).
- [x] P11-02 `ISinkHealthProbe.cs`, `SinkHealth.cs`, `HealthAggregator.cs`. `FileRollingSink` and `SqliteSink` now implement the probe via `*.Health.cs` partials.
- [x] P11-03 Wire counters from each pipeline stage:
  - `TelemetryPipeline.Drain` increments enqueued / sampled-out / deduped / rate-limited / queue-full.
  - `BatchWriter` records per-sink errors and per-batch flush latency via callbacks.
  - `BeepAudit.Core` increments `chain_signed_total`; `BeepAudit.Query` increments `chain_verified_total` and `chain_divergence_total`.
  - `PipelineDiagnosticsHooks` subscribes to `IBudgetEnforcer.Swept` (sweeper deletes/compress + budget breaches) and `TelemetryPipeline.SinkErrored` (sink errors + self events).
- [x] P11-04 `SelfEventEmitter` with per-(category, key) dedup window, bypassing dedup/rate-limit/sampler stages by routing through `EnqueueSelfEvent`.
- [x] P11-05 `PeriodicMetricsSnapshotHostedService` (opt-in) + `MetricsSnapshotRenderer` (text/JSON) + `MetricsSnapshotFormat` enum. Atomic temp+rename writes for crash safety.
- [ ] P11-06 Tests for counters, self events, and snapshot writer (deferred to Phase 13).

## Operator surface delivered

```csharp
services.AddBeepLogging(o =>
{
    o.Enabled = true;
    o.EnableMetricsSnapshot       = true;            // opt-in hosted service
    o.MetricsSnapshotInterval     = TimeSpan.FromMinutes(5);
    o.MetricsSnapshotFile         = "metrics.txt";
    o.MetricsSnapshotFormat       = MetricsSnapshotFormat.Text;
    o.EmitMetricsSnapshotAsSelfEvent = false;        // also push as self event
});
services.AddBeepAudit(o =>
{
    o.Enabled = true;
    o.EnableMetricsSnapshot       = true;
    o.MetricsSnapshotFormat       = MetricsSnapshotFormat.Json;
});

// Read counters at any time:
var pipeline = sp.GetRequiredKeyedService<TelemetryPipeline>("Beep.Logging.Pipeline");
MetricsSnapshot snap = pipeline.Metrics.Snapshot();
```

## Build status

- `net8.0`, `net9.0`, `net10.0` — all green, zero new warnings on Phase 11 files.

## Verification

- Filling the queue raises `dropped_total[queue-full]` and emits exactly one self event per minute.
- Killing a sink target (e.g. delete the directory) flips `IsHealthy = false` and emits one self event.
- Snapshot file appears at the configured interval and is valid JSON / readable text.
- With self-metrics off, no extra threads or files are produced.

## Risks

- **R1**: Self events feeding back into the pipeline could amplify under failure. Mitigation: hard rate-limit + dedupe + bypass sinks that are unhealthy by routing self events to `MemorySink` fallback.
- **R2**: Counter contention under high throughput. Mitigation: per-thread `Interlocked` counters aggregated in `Snapshot()`.

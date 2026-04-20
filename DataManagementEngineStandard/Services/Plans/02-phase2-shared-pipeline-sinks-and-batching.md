# Phase 02 — Shared Pipeline, Sinks & Async Batching

## Objective

Build the **shared telemetry pipeline** that both `IBeepLog` and `IBeepAudit` ride on: bounded queue, async batched writer, sink fan-out, backpressure policy, and graceful flush. This is the storage-and-CPU contract that makes the rest of the program safe.

## Dependencies

- Phase 01 contracts compiled.

## Scope

- **In**: Pipeline core, queue, batch writer, sink contract, no-op + memory sinks for unit tests, hook-up to `BeepLog` / `BeepAudit`.
- **Out**: Real disk/SQLite sinks (Phase 03), retention (Phase 04), redaction (Phase 05), enrichment (Phase 06), sampling (Phase 07).

## Target files

```
Services/Telemetry/
  TelemetryEnvelope.cs
  TelemetryKind.cs                    // Log | Audit
  ITelemetrySink.cs
  IEnricher.cs                        // forward-decl, used in Phase 06
  IRedactor.cs                        // forward-decl, used in Phase 05
  ISampler.cs                         // forward-decl, used in Phase 07
  BoundedChannelQueue.cs
  BatchWriter.cs
  TelemetryPipeline.cs                // partial: .Core, .Drain, .Flush
  TelemetryPipeline.Core.cs
  TelemetryPipeline.Drain.cs
  TelemetryPipeline.Flush.cs

Services/Logging/
  BeepLog.cs                          // real impl, partial: .Core, .Levels, .Lifetime
  BeepLog.Core.cs
  BeepLog.Levels.cs
  BeepLog.Lifetime.cs

Services/Audit/
  BeepAudit.cs                        // partial: .Core, .Query, .Lifetime
  BeepAudit.Core.cs
  BeepAudit.Query.cs                  // throws NotImplemented until Phase 10
  BeepAudit.Lifetime.cs
```

## Design notes

### `TelemetryEnvelope`

Single record type carried from producer to sink. One envelope class avoids two parallel queues (logs + audit share queue capacity but never get re-ordered relative to each other).

```csharp
public sealed class TelemetryEnvelope
{
    public TelemetryKind Kind        { get; init; }
    public DateTime      TimestampUtc { get; init; } = DateTime.UtcNow;
    public string        Category     { get; init; }
    public LogLevel      Level        { get; init; }
    public string        Message      { get; init; }
    public Exception     Exception    { get; init; }
    public IDictionary<string, object> Properties { get; init; }
    public AuditEvent    Audit        { get; init; }   // null when Kind = Log
    public string        TraceId      { get; init; }
    public string        CorrelationId{ get; init; }
}
```

### Bounded queue

Wraps `System.Threading.Channels.Channel<TelemetryEnvelope>` with `BoundedChannelOptions { Capacity, FullMode = Wait | DropOldest | DropWrite }`. Mode is selected from `BackpressureMode`.

### `BatchWriter`

A long-running task per pipeline that:

1. Awaits next item, then drains up to `MaxBatchSize` (default 256) or `FlushInterval` (default 2 s) items.
2. Calls `sink.WriteBatchAsync(batch, ct)` for each registered sink.
3. Catches per-sink exceptions, increments `PipelineMetrics.SinkErrors[sinkName]`, never crashes the pipeline.
4. Honors `ShutdownTimeout` for cooperative drain on stop.

### `ITelemetrySink`

```csharp
public interface ITelemetrySink : IAsyncDisposable
{
    string Name { get; }
    Task WriteBatchAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken ct);
    Task FlushAsync(CancellationToken ct);
    bool IsHealthy { get; }
}
```

### Backpressure semantics (locked)

| Kind | Default mode | Behavior on full queue |
|---|---|---|
| Log   | `DropOldest` | Oldest logs dropped first; drop counter incremented; warning surfaced through `PipelineMetrics`. |
| Audit | `Block`      | Producer blocks; if blocked for > `ShutdownTimeout / 2`, mode escalates to `FailFast` and producer receives an exception (caller decides whether to drop the user action). |

Audit *never* uses `DropOldest`. Logs *never* use `Block` by default (operator may opt in for compliance modes, e.g. financial trail logs).

### `TelemetryPipeline` partials

- `.Core`        — fields, ctor, sinks/enrichers/redactors/samplers wiring.
- `.Drain`       — the `BatchWriter` loop.
- `.Flush`       — `FlushAsync(timeout)` that drains the queue and awaits each sink's flush.

### `BeepLog` / `BeepAudit` partials

- `.Core`     — pipeline reference + `IsEnabled` short-circuit.
- `.Levels` (Log only) — `Trace/Debug/Info/Warn/Error/Critical` thin wrappers around `Log(level, category, ...)`.
- `.Lifetime` — `FlushAsync` delegated to pipeline; `Dispose` semantics.

## Implementation steps

1. Add envelope, kind, sink contract, forward-declared interfaces.
2. Implement `BoundedChannelQueue` (selectable `BoundedChannelFullMode`).
3. Implement `BatchWriter` as a `Task` started by the pipeline ctor.
4. Implement `TelemetryPipeline` (3 partial files).
5. Wire up `BeepLog` and `BeepAudit` to the pipeline.
6. Extend `BeepServiceExtensions.{Logging,Audit}` from Phase 01 so when `Enabled=true` they construct the real implementations using the configured sinks/enrichers/redactors/samplers.
7. Add unit tests using a `MemorySink` (Phase 03 will add the production memory sink — for Phase 02 a simple in-file `RecordingSink` test double is enough).

## TODO checklist

- [ ] P02-01 `TelemetryEnvelope.cs` + `TelemetryKind.cs`.
- [ ] P02-02 `ITelemetrySink.cs` + forward-decl interfaces.
- [ ] P02-03 `BoundedChannelQueue.cs`.
- [ ] P02-04 `BatchWriter.cs`.
- [ ] P02-05 `TelemetryPipeline.{Core,Drain,Flush}.cs`.
- [ ] P02-06 `BeepLog.{Core,Levels,Lifetime}.cs` + `BeepAudit.{Core,Query,Lifetime}.cs`.
- [ ] P02-07 Wire pipeline construction into `AddBeepLogging`/`AddBeepAudit`.
- [ ] P02-08 Unit tests covering: enqueue, batch flush, drop-oldest under load, block-then-fail for audit, graceful shutdown drain.

## Verification

- 100k logs/sec sustained on a single pipeline with `MemorySink` and 4 enrichers (no sinks doing IO yet).
- Audit producer blocks (does not drop) under simulated full queue.
- `FlushAsync(TimeSpan.FromSeconds(5))` drains every queued envelope before returning.
- With features off, no pipeline is constructed and there is zero queued/threaded work.

## Risks

- **R1**: Channel `DropOldest` cost. Mitigation: keep capacity at 10k by default; document tuning.
- **R2**: A single misbehaving sink could starve others. Mitigation: per-sink try/catch and a future per-sink dispatcher in Phase 11 if needed.
- **R3**: Cross-platform `Channel<T>` perf is good on .NET 8+; verify on MAUI/Blazor in Phase 12.

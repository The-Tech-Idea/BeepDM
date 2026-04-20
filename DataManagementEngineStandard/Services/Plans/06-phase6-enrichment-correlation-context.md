# Phase 06 — Enrichment, Correlation & Ambient Context

## Objective

Make every log and audit envelope automatically carry **who / where / why / when** without forcing the caller to plumb fields manually. Add ambient `BeepActivityScope` (AsyncLocal-based) and a stack of opt-in `IEnricher`s.

## Dependencies

- Phase 02 pipeline (enrichers run between producer and queue).

## Scope

- **In**: Activity scope, enricher contract, built-in enrichers, OTel `Activity` bridge.
- **Out**: Distributed propagation across processes (handled by OTel exporter in Phase 13/v2).

## Target files

```
Services/Telemetry/Context/
  BeepActivityScope.cs               // partial: .Core, .AsyncLocal
  BeepActivityScope.Core.cs
  BeepActivityScope.AsyncLocal.cs
  BeepActivity.cs                    // POCO: name, traceId, spanId, parentSpanId, startUtc, tags
  IEnricher.cs                       // moved from Phase 02 forward-decl
  CorrelationEnricher.cs
  TraceEnricher.cs
  MachineProcessEnricher.cs
  EnvironmentEnricher.cs             // env name, region, version
  TenantEnricher.cs                  // pulled from BeepService.AppRepoName / IDMEEditor
  UserEnricher.cs                    // pulled from ambient principal / BeepService context
  ActivityScopeEnricher.cs
```

## Design notes

### `BeepActivityScope`

Lightweight, AsyncLocal-backed scope stack. Compatible with `System.Diagnostics.Activity` if one is current, otherwise generates its own ids.

```csharp
public static class BeepActivityScope
{
    public static IDisposable Begin(string name, IDictionary<string, object> tags = null);
    public static BeepActivity Current { get; }
}
```

Internally:
- `AsyncLocal<Stack<BeepActivity>>`.
- `Begin` pushes a new activity, generates `traceId`/`spanId` (16-byte and 8-byte hex per OTel convention).
- If `System.Diagnostics.Activity.Current` exists, inherit `TraceId`/`SpanId` from it.
- `Dispose` pops the activity (no-op if mismatch).

### Enrichment order

Enrichers run sequentially before redaction (envelope properties are populated, then redactors run, then samplers, then enqueue).

### Built-in enrichers

| Enricher | Adds |
|---|---|
| `CorrelationEnricher`     | `correlationId` (from scope or new GUID-N) |
| `TraceEnricher`           | `traceId`, `spanId`, `parentSpanId` |
| `ActivityScopeEnricher`   | `scopeName`, `scopeStartUtc`, `scopeTags` |
| `MachineProcessEnricher`  | `machine`, `processId`, `processName`, `threadId` |
| `EnvironmentEnricher`     | `envName`, `region`, `appVersion`, `appRepoName` |
| `TenantEnricher`          | `tenant` (from `IBeepService.AppRepoName` or operator-provided resolver) |
| `UserEnricher`            | `userId`, `userName` (from operator-provided `Func<string?>`) |

All are **opt-in** via `BeepLoggingOptions.Enrichers.Add(...)` and `BeepAuditOptions.Enrichers.Add(...)`.

### OTel bridge

`TraceEnricher` reads `System.Diagnostics.Activity.Current` first; if present, uses its trace context. This guarantees correlation with any framework already producing OTel spans (ASP.NET Core, gRPC, etc.) and makes the future OTel exporter trivial.

### Storage cost note

Enrichment fields are usually short (≤ 32 bytes each). Default enricher set adds ≈ 200 bytes per envelope; document this when sizing `StorageBudgetBytes` in Phase 04.

## Implementation steps

1. Implement `BeepActivity`, `BeepActivityScope`.
2. Move `IEnricher` from forward-decl to canonical location.
3. Implement the seven built-in enrichers (one file per class).
4. Wire enricher application into `TelemetryPipeline.Drain` *before* redaction.
5. Add OTel `Activity` propagation to `TraceEnricher`.
6. Helper extension `using (this.Scope("OperationName")) { ... }` on `IBeepLog`.
7. Tests for: scope nesting, AsyncLocal propagation across `await`, OTel inherit, no-op when not opted in.

## TODO checklist

- [ ] P06-01 `BeepActivity.cs`, `BeepActivityScope.{Core,AsyncLocal}.cs`.
- [ ] P06-02 `IEnricher.cs` (canonical).
- [ ] P06-03 Built-in enrichers (7 files).
- [ ] P06-04 Pipeline wiring (enrichment → redaction → sampling → queue).
- [ ] P06-05 OTel `Activity` inherit in `TraceEnricher`.
- [ ] P06-06 `IBeepLog.Scope` extension method.
- [ ] P06-07 Tests for AsyncLocal across `await`, scope nesting, OTel inherit.

## Verification

- An `await`-laden code path keeps the same `correlationId` end to end.
- When ASP.NET Core produces an OTel `Activity`, the log envelopes inherit its `traceId`/`spanId`.
- With zero enrichers configured, envelopes are still valid and only carry producer-supplied fields.

## Risks

- **R1**: AsyncLocal misuse can leak context across requests in long-lived hosts. Mitigation: scope strictly disposed; sample apps demonstrate `using` blocks.
- **R2**: `Activity` API surface differs slightly across .NET versions. Mitigation: use the API common since .NET 6; we already require modern TFMs.

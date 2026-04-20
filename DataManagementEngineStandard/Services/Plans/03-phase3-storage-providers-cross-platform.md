# Phase 03 — Storage Providers (Cross-Platform)

## Objective

Ship the cross-platform sink set: in-memory, null, file (rolling NDJSON), SQLite, composite. Every disk/file API used must work on Windows, Linux, macOS, MAUI, and (where applicable) Blazor WebAssembly.

## Dependencies

- Phase 02 pipeline + `ITelemetrySink`.

## Scope

- **In**: Sinks listed below + cross-platform path resolution.
- **Out**: Rotation/retention/compression policies (Phase 04 — sinks expose write/flush hooks; sweeper + budget are external).

## Target files

```
Services/Telemetry/Sinks/
  MemorySink.cs
  NullSink.cs
  FileRollingSink.cs                 // partial: .Core, .Write, .Roll
  FileRollingSink.Core.cs
  FileRollingSink.Write.cs
  FileRollingSink.Roll.cs
  SqliteSink.cs                      // partial: .Core, .Schema, .Write, .Query
  SqliteSink.Core.cs
  SqliteSink.Schema.cs
  SqliteSink.Write.cs
  SqliteSink.Query.cs
  CompositeSink.cs

Services/Telemetry/
  PlatformPaths.cs
  NdjsonSerializer.cs
```

## Design notes

### `PlatformPaths`

Single source of truth for "where can we safely write on this OS / host?". Returns directories using only portable APIs:

```csharp
public static class PlatformPaths
{
    public static string AppDataRoot(string appName)
        => Path.Combine(
             Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData,
                                       Environment.SpecialFolderOption.Create),
             appName);

    public static string LogsDir(string appName, string subfolder = "logs")
        => Path.Combine(AppDataRoot(appName), subfolder);
}
```

For Blazor WASM, this class is overridden in Phase 12 to point to a JS-interop IndexedDB shim (`PlatformPaths` returns a virtual path; `IndexedDBSink` ignores the path and stores in IDB).

### `NdjsonSerializer`

UTF-8 newline-delimited JSON. Uses `System.Text.Json` (cross-platform). Handles `Exception` shape via a flat property bag, not full reflection on the type tree.

### `MemorySink`

In-process ring buffer (`ConcurrentQueue<TelemetryEnvelope>` capped at `MaxItems`). Used for tests and as a low-storage fallback.

### `NullSink`

Discards everything. Useful as the default sink when feature is enabled but operator has not configured sinks (so the pipeline still exercises enrichers/redactors).

### `FileRollingSink`

- Open-on-first-write file `logs/{prefix}-{utc-yyyymmdd-hhmmss}.ndjson`.
- After each batch: if `currentFileBytes >= MaxFileBytes` OR wall-clock crosses `RollIntervalMinutes`, close + rotate. Rotation triggers a hook consumed by Phase 04 (compression + retention).
- Append using `FileStream(FileMode.Append, FileAccess.Write, FileShare.Read)`.
- Buffered writer with explicit flush on `FlushAsync`.

### `SqliteSink`

- Single file `audit.db` (or `logs.db`). Uses `Microsoft.Data.Sqlite` (cross-platform).
- Tables: `telemetry(id, kind, ts, level, category, msg, props_json, trace_id, corr_id, audit_json)`.
- Indexes on `(kind, ts)`, `(category, ts)`, and (for audit) `(audit_user, ts)`, `(audit_entity, ts)`.
- WAL mode + `synchronous = NORMAL` for throughput; `synchronous = FULL` toggle for audit-strict mode.
- Batched insert in a transaction per write batch (one round-trip per `WriteBatchAsync`).

### `CompositeSink`

Fan-out to multiple sinks. Errors per inner sink are isolated and surfaced via `IsHealthy`.

## Implementation steps

1. Add `PlatformPaths` + `NdjsonSerializer` (no per-OS branches; let .NET handle paths).
2. Add `MemorySink` and `NullSink`.
3. Add `FileRollingSink` partials (no compression yet — emits a `Rolled(file)` event for Phase 04).
4. Add `SqliteSink` partials with WAL + batched transaction.
5. Add `CompositeSink`.
6. Smoke tests for each sink (write 1k envelopes; verify shape; verify flush idempotence).

## TODO checklist

- [ ] P03-01 `PlatformPaths.cs`.
- [ ] P03-02 `NdjsonSerializer.cs`.
- [ ] P03-03 `MemorySink.cs`, `NullSink.cs`.
- [ ] P03-04 `FileRollingSink.{Core,Write,Roll}.cs`.
- [ ] P03-05 `SqliteSink.{Core,Schema,Write,Query}.cs`.
- [ ] P03-06 `CompositeSink.cs`.
- [ ] P03-07 Smoke tests for each sink.

## Verification

- All sinks build and run on Windows, Linux, macOS (CI matrix).
- `FileRollingSink` writes a valid NDJSON file (each line parses as JSON).
- `SqliteSink` survives process kill mid-batch (WAL recovery on next open) — covered by integration test.
- `PlatformPaths.LogsDir("Beep")` resolves to a real, creatable directory on every supported OS.

## Risks

- **R1**: `Microsoft.Data.Sqlite` dependency adds ~1.5 MB to the package. Mitigation: keep `SqliteSink` in a separate folder; document that it is only loaded if registered.
- **R2**: NDJSON writes can be slow at very high rate. Mitigation: BatchWriter coalesces; future phase 14-style adaptive flush interval (deferred).
- **R3**: File handles on mobile may be limited. Mitigation: one open file per sink per process, never per envelope.

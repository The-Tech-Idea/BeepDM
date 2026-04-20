# Beep Logging & Audit — CHANGELOG

Per-phase deltas. Mirrors the MASTER tracker so each `[x]` flips here
when a phase ships. Dates use ISO-8601.

---

## v1.0.0 — Phase 13 — DevEx, Testing, Rollout & Docs (2026-04-19)

Closeout phase. Adds operator-facing tooling, sample apps, and the
public docs that make this a production-grade feature.

- New: `Services/Telemetry/Sinks/Test/RecordingSink.{Core,Query}.cs` —
  in-memory queryable test sink with typed `Logs()` / `Audit()` /
  `WithCategory()` / `SelfEvents()` / `FirstLog()` helpers.
- New: `Services/Telemetry/Sinks/Test/FlakySink.cs` — deterministic
  failure on every Nth batch; optional inner sink to forward
  successes.
- New: `Services/Telemetry/Sinks/Test/SlowSink.cs` — `Task.Delay`-based
  latency injection so the bounded queue can be driven into its
  back-pressure path.
- New: `Services/Telemetry/Sinks/Test/FullDiskSink.cs` —
  storage-exhaustion injection with `MaxBytes` / `UsedBytes` /
  `IsFull` and a `ResetUsage()` recovery hook.
- New: `Services/Telemetry/Diagnostics/PerfHarness.cs` +
  `PerfHarnessResult` — fires N envelopes/sec for a configured
  duration and reports throughput, drop counts, peak queue depth, and
  end-to-end latency observed at a downstream `MemorySink`.
- New: `Services/Examples/LoggingDesktopExample.cs`,
  `LoggingWebApiExample.cs`, `LoggingBlazorExample.cs`,
  `AuditFormsExample.cs`, `AuditDistributedExample.cs` — five
  copy-pasteable sample programs showing DI registration, redaction,
  correlation scopes, periodic flush + clean shutdown, and integrity
  verification.
- New: `Services/Plans/RUNBOOK.md` — operator playbook covering
  enable/disable, budget tuning, integrity verification, corrupt-chain
  recovery, GDPR purge, key rotation, clean shutdown, and a triage
  matrix for the most common failures.
- New: `Services/Plans/CHANGELOG.md` (this file).
- Build: net8.0 + net9.0 + net10.0 build succeeds with **0 errors**;
  no new lint warnings on any new file under
  `Services/Telemetry/Sinks/Test/`, `Services/Telemetry/Diagnostics/`
  or `Services/Examples/`.

---

## v0.12.0 — Phase 12 — Platform Targets (2026-04-19)

- New: `Services/Telemetry/Presets/PlatformBudgets.cs` — single source
  of truth for per-platform log + audit byte caps, queue capacities,
  and per-file rotation caps.
- New: `Services/Telemetry/Sinks/Platform/IIndexedDbBridge.cs` and
  `BlazorIndexedDbSink.{Core,Write,Health}.cs` — Blazor WASM sink
  that delegates JS interop to the host through a typed bridge.
- New: `Services/Telemetry/Sinks/Platform/MauiAppDataSink.cs` —
  static factory wrapping `FileRollingSink` with MAUI-tuned defaults
  (1 MB rotation, 15 min interval, no gzip).
- New: `BeepServiceExtensions.{Desktop,Web,Blazor,Maui}.cs` — six new
  `AddBeepLogging|AuditFor*` methods with platform-locked defaults.
- Library still takes **zero** hard dependencies on
  `Microsoft.JSInterop`, `Microsoft.Maui.Storage`, or
  `Microsoft.AspNetCore.Hosting`.

---

## v0.11.0 — Phase 11 — Self-Observability & Health (2026-04-18)

- New: `PipelineMetrics.{Core,Counters,Gauges,Snapshot}.cs` +
  `MetricsSnapshot` POCO — `Interlocked`-backed counters with no
  dynamic allocations on the hot drain path.
- New: `ISinkHealthProbe`, `SinkHealth`, `HealthAggregator`.
  `FileRollingSink` and `SqliteSink` implement the probe.
- New: `SelfEventEmitter`, `SelfEventCategory`,
  `PipelineDiagnosticsHooks` — internal events surface under
  `BeepTelemetry.Self.*` and bypass dedup / rate-limit / sampler.
- New: `PeriodicMetricsSnapshotHostedService`,
  `MetricsSnapshotRenderer`, `MetricsSnapshotFormat` — opt-in
  periodic text/JSON snapshots written atomically.
- `TelemetryPipeline` exposes `Name`, `Metrics`, `EnqueueSelfEvent`
  and a `SinkErrored` event.
- `BatchWriter` accepts `onSinkError` / `onFlushLatency` callbacks.

---

## v0.10.0 — Phase 10 — Query, Export, Purge & Compliance (2026-04-17)

- New: `AuditQuery.{Core,Filters}.cs` builder + in-memory `Matches`
  matcher reused by every engine.
- New: `IAuditQueryEngine` + `SqliteAuditQueryEngine`,
  `FileScanAuditQueryEngine`, `CompositeAuditQueryEngine`,
  `NdjsonAuditDeserializer`.
- New: `AuditExporter.{Core,Csv,Json,Ndjson}.cs` + `ExportManifest`,
  `ManifestSigner` (HMAC-SHA256 over canonical manifest), atomic
  `<file>` + `<file>.manifest.json` writes.
- New: `IPurgePolicy` + `ConfirmTokenPurgePolicy` +
  `GdprPurgeService.{Core,User,Entity,ResealChain}.cs`,
  `IAuditPurgeStore`, `PurgeImpact`, `HashChainSigner.Reseal.cs` —
  GDPR purge re-signs surviving chain segments and writes a
  synthetic `Custom`/`PurgeBy{User|Entity}` audit event.
- New: `LogQuery`, `LogRecord`, `ILogQueryEngine` +
  `SqliteLogQueryEngine`, `FileScanLogQueryEngine`,
  `CompositeLogQueryEngine`, `NdjsonLogDeserializer`.

---

## v0.9.0 — Phase 09 — Integration With Existing Subsystems (2026-04-16)

- New: `DMLoggerToBeepLogBridge` — legacy `lg.WriteLog(...)` flows
  through the unified pipeline.
- New: `MicrosoftLoggerProvider` + `MicrosoftLoggerAdapter` +
  `MicrosoftLoggerScope` — exposes `IBeepLog` as an
  `ILoggerProvider`; MEL `BeginScope` lifts into
  `BeepActivityScope.Begin`.
- New: `FormsAuditBridge`, `ProxyAuditBridge`,
  `DistributedAuditBridge`, `AuditBridgeRegistry`,
  `AuditStoreSaveExtensions.SaveAndForward` — append-only forwarding,
  no breaking changes to legacy stores.

---

## v0.8.0 — Phase 08 — Audit Event Schema & Tamper Evidence (2026-04-15)

- New: `AuditEvent.ChainFields.cs` partial — `Category`,
  `Operation`, `Outcome`, `Reason`, `FieldChanges`, `ChainId`,
  `Sequence`, `PrevHash`, `Hash`.
- New: `AuditCategory`, `AuditOutcome`, `AuditFieldChange`.
- New: `IHashChainSigner` + `HashChainSigner.{Core,Sign,Verify}.cs`
  partials with per-`ChainId` mutex map and canonical JSON via
  `CanonicalJsonSerializer`.
- New: `IKeyMaterialProvider` (`Environment*` and `Static*`),
  `IChainAnchorStore` (`JsonChainAnchorStore` with atomic
  `tmp + Replace/Move`).
- New: `IntegrityVerifier`, `IAuditEventReader`,
  `IntegrityIssueKind`, `IntegrityIssue`, `IntegrityCheckResult`.
- New: `SealedLogPolicy` — `FileAttributes.ReadOnly` on Windows
  + `chmod 0440` on Linux/macOS.
- Hash chain ON by default (`BeepAuditOptions.HashChain = true`).

---

## v0.7.0 — Phase 07 — Sampling, Dedup & Rate Limiting (2026-04-14)

- New: `ISampler` + `LevelSampler`, `CategorySampler`,
  `AlwaysSampler`, `NeverSampler`; deterministic FNV-1a hashing of
  correlation/trace ids.
- New: `IMessageDeduper` + `WindowedDeduper.{Core,Window}.cs` +
  `MessageTemplateNormalizer` — LRU-capped, time-windowed
  deduplication that emits a synthetic summary on window expiry.
- New: `IRateLimiter` + `TokenBucketRateLimiter` — per-key token
  bucket with periodic `[rate-limited]` summary envelope.
- Pipeline order locked: enrich → redact → sample → dedup →
  rate-limit → queue.
- Audit deliberately bypasses sampler / dedup / rate-limit.

---

## v0.6.0 — Phase 06 — Enrichment & Correlation (2026-04-13)

- New: `BeepActivityScope.{Core,AsyncLocal}.cs` +
  `BeepActivity` POCO + `IdGenerators` (W3C-compatible 32/16 hex).
- New: `CorrelationEnricher`, `TraceEnricher`,
  `MachineProcessEnricher`, `EnvironmentEnricher`,
  `ActivityScopeEnricher`, `TenantEnricher`, `UserEnricher`.
- `TraceEnricher.TryReadAmbient` prefers
  `System.Diagnostics.Activity.Current` for OTel inheritance.
- New: `BeepLogScopeExtensions.Scope(name, tags)`.

---

## v0.5.0 — Phase 05 — Redaction, PII & Secret Scrubbing (2026-04-12)

- New: `RedactionMode`, `RedactionContext`, `RedactionHelpers`,
  `TelemetryEnvelope.Clone()`.
- New: `RegexRedactor`, `KeywordRedactor`, `KeyValueRedactor`,
  `StructuredFieldRedactor`, `ProxyRedactorAdapter`.
- New: `ConnectionStringRedactor`, `JwtRedactor`,
  `CreditCardRedactor` (Luhn pre-filter), `EmailRedactor`,
  `CompositeRedactor`, `DefaultRedactionPresets` (`Off`,
  `LogsBalanced`, `AuditStrict`).
- New: `RedactingSinkDecorator` — per-sink redactor stacks operate
  on cloned envelopes.

---

## v0.4.0 — Phase 04 — Retention, Rotation, Compression, Budget (2026-04-11)

- New: `RotationPolicy`, `RetentionPolicy`, `StorageBudget`,
  `BudgetBreachAction`.
- New: `IBudgetEnforcer` + `DefaultBudgetEnforcer.{Core,Sweep,Compress}.cs`
  + `EnforcerScope`, `BudgetSweepResult`.
- New: gzip on rotate (`DefaultBudgetEnforcer.Compress.cs` subscribes
  to `FileRollingSink.Rolled`).
- New: `RetentionSweeperHostedService` — opt-in via
  `AddBeepRetentionSweeper(TimeSpan?)`.
- Logs default to `DeleteOldest`; audit defaults to
  `BlockNewWrites`.

---

## v0.3.0 — Phase 03 — Storage Providers (2026-04-10)

- New: `MemorySink` (ring buffer), `NullSink`, `CompositeSink`.
- New: `FileRollingSink.{Core,Write,Roll}.cs` partials +
  `RolledFile` event payload + `NdjsonSerializer`.
- New: `SqliteSink.{Core,Schema,Write,Query}.cs` partials with WAL +
  per-batch transactions; depends on `Microsoft.Data.Sqlite 10.0.6`.
- New: `PlatformPaths` — `LocalApplicationData` resolver for
  `LogsDir` / `AuditDir`.

---

## v0.2.0 — Phase 02 — Shared Pipeline, Sinks, Async Batching (2026-04-09)

- New: `TelemetryEnvelope`, `TelemetryKind`, `ITelemetrySink`,
  forward-decl `IEnricher`, `IRedactor`, `ISampler`.
- New: `BoundedChannelQueue` (DropOldest / Block / FailFast),
  `BatchWriter` (per-sink try/catch isolation, drain-on-dispose),
  `TelemetryPipeline.{Core,Drain,Flush}.cs` partials.
- New: `BeepLog.{Core,Levels,Lifetime}.cs`,
  `BeepAudit.{Core,Query,Lifetime}.cs` — production
  implementations backed by the pipeline.
- Logging and audit each get their own pipeline registered as keyed
  singletons (`Beep.Logging.Pipeline`, `Beep.Audit.Pipeline`).

---

## v0.1.0 — Phase 01 — Core Contracts & Feature Toggles (2026-04-08)

- New: `IBeepLog`, `BeepLogLevel`, `IBeepAudit`, `AuditEvent`,
  `AuditQuery`.
- New: `BeepLoggingOptions`, `BeepAuditOptions`,
  `AddBeepLogging`, `AddBeepAudit`.
- New: `NullBeepLog`, `NullBeepAudit` — zero-allocation no-ops when
  the feature is disabled.
- New: `BackpressureMode`, `TelemetryFeature` constants seeded for
  Phase 02.

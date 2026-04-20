# Phase 00 — Overview & Gap Matrix

## Objective

Produce the authoritative inventory of every existing logging and audit touchpoint in the Beep stack, identify gaps against the v1 vision (`README.md`), and freeze scope for Phases 01–13.

## Scope

- **In**: Read-only review and decision making. Producing the gap matrix table. Locking scope.
- **Out**: Any code change. Any contract design (that is Phase 01).

## Existing components — current state

| Layer | Component | What it does today | Limitations |
|---|---|---|---|
| Models | `IDMLogger` (`DataManagementModelsStandard/Logger/IDMLogger.cs`) | Defines basic `WriteLog/LogError/.../LogStructured`, `Onevent` event, filter list, configure delegate | Free-form strings only; no batching; no sinks; no async; no cross-platform storage policy |
| Engine | `DMLogger` (`DataManagementEngineStandard/Logger/DMLogger.cs`) | Wraps `Microsoft.Extensions.Logging` factory + console provider; bridges `Microsoft.Extensions.Logging.ILogger`; supports filters and start/stop/pause | Console only by default; no rotation; no audit semantics; no PII redaction at the engine level |
| Engine | `LogAndError.cs` | Legacy enum/struct for log+error compound events | Older shape; not aligned with structured event envelope |
| Forms | `AuditManager` + `IAuditStore` + `FileAuditStore` + `InMemoryAuditStore` (`Editor/Forms/Helpers/`) | Record field-level changes per block, flush on commit, query/export to CSV/JSON, retention purge | Form/block-scoped; whole-file rewrites on each save; no async batching; no compression; no tamper evidence; no cross-feature reuse |
| Proxy | `IProxyAuditSink`, `FileProxyAuditSink`, `NullProxyAuditSink`, `ProxyLogRedactor` (`Proxy/`) | Records proxy node events; redacts secrets in connection strings | Proxy-only; not consumable by app code; redactor not reusable outside proxy |
| Distributed | (planned) `IDistributedAuditSink` in `DistributedDatasource/DistributedPlans/13-...md` | Aggregated metrics + extended audit + durable transaction log | Not yet implemented; will need to plug into the unified audit service rather than build its own |
| Services | `BeepService` (`Services/BeepService.cs`) | Composition root; exposes `lg : IDMLogger` | No `IBeepAudit`, no opt-in toggle for unified logging, no platform-aware defaults |
| Service registration | `BeepServiceExtensions.{Desktop,Web,Blazor}.cs` | Registers core Beep services per host | No `AddBeepLogging` / `AddBeepAudit` extension points |

## Gap matrix (current → v1 target)

| Capability | Today | v1 target | Phase |
|---|---|---|---|
| Unified, structured log envelope | None — strings only | `TelemetryEnvelope` + `BeepLogEntry` (level, category, message, props, ts, traceId, correlationId, exception) | 02 |
| Async batched writers | No | `BoundedChannelQueue` + `BatchWriter` with backpressure policies | 02 |
| File rolling sink | Per-feature ad-hoc | Cross-platform `FileRollingSink` (NDJSON, size+time roll) | 03 |
| SQLite sink | No | `SqliteSink` (single file, WAL) for queryable logs/audit | 03 |
| Compression on rotate | No | Gzip on rotate | 04 |
| Retention by days + max files | Forms-only `Purge(days)` | `RetentionPolicy` + `IBudgetEnforcer` | 04 |
| Total storage cap | No | `StorageBudget` per directory + sweeper | 04 |
| Redaction / PII | Proxy only (`ProxyLogRedactor`) | `IRedactor` pipeline; built-in regex/keyword/structured | 05 |
| Correlation/trace context | No | `BeepActivityScope` (AsyncLocal) + OTel `Activity` bridge | 06 |
| Sampling / dedup / rate-limit | No | `LevelSampler` / `MessageDeduper` / `TokenBucketRateLimiter` | 07 |
| Canonical audit schema | Per-source ad-hoc | `AuditEvent v1` with `AuditCategory` | 08 |
| Tamper-evident chain | No | HMAC-SHA256 hash chain + `IntegrityVerifier` | 08 |
| Bridges from existing emitters | Hard-coded | `DMLoggerToBeepLogBridge`, `FormsAuditBridge`, `ProxyAuditBridge`, `DistributedAuditBridge` | 09 |
| Query / export / GDPR purge | Forms only | Unified `IAuditQuery` + `AuditExporter` + `PurgeByUser/Entity` | 10 |
| Self-observability | No | `PipelineMetrics` + sink health probes | 11 |
| Platform-aware defaults | No | Desktop / Web / Blazor / MAUI extension methods with budgeted defaults | 12 |
| Test/dev tooling | Manual | `RecordingSink`, `FlakySink`, perf harness, sample apps | 13 |

## v1 design decisions (locked)

1. **One unified pipeline** under `Services/Telemetry/` shared by both `IBeepLog` and `IBeepAudit`.
2. **Opt-in by default**. `BeepService` runs unchanged unless `AddBeepLogging` / `AddBeepAudit` is called.
3. **Backwards compatible**. Existing `IDMLogger` and `AuditManager` continue to work; bridges forward to the new pipeline only when feature is enabled.
4. **Cross-platform constraint**: only `System.IO`, `System.Threading.Channels`, `System.IO.Compression`, `System.Diagnostics.Activity`, `Microsoft.Data.Sqlite`. No `EventLog`, no `PerformanceCounter`, no Windows path assumptions.
5. **Storage-conscious**: every sink that writes to disk MUST honor a `StorageBudget`; logs use `DropOldest` under pressure, audit uses `Block` then `FailFast` if budget breaches even after sweep.
6. **Lossy logs / lossless audit**: codified at the pipeline level; sampler/dedup/rate-limiter never operate on audit events.
7. **Single MASTER tracker**, one phase doc per phase, one class per file, partial classes for orchestrators (per user rules).

## v1 non-goals

- Building a SIEM UI or hosted log analytics.
- Replacing `Microsoft.Extensions.Logging` — we plug in.
- Per-tenant routing fabric (single-process tenant *tagging* only).
- At-rest encryption of payloads (v2; we ship redaction + tamper evidence in v1).
- Remote log shipping over a custom protocol (OTel exporter is the v1 escape hatch).

## TODO checklist

- [ ] P00-01 Inventory all existing logging touchpoints.
- [ ] P00-02 Inventory all existing audit touchpoints.
- [ ] P00-03 Produce gap matrix and v1 scope freeze.
- [ ] P00-04 Define non-goals and v2 deferrals.

## Verification

- Each row in the gap matrix maps to at least one phase.
- Every "today" cell links to an actual file in the repo (paths above all resolve).
- Decisions section accepted and locked before Phase 01 starts.

## Risks

- **R1**: Hidden third-party emitters bypass the bridges — mitigated by the `ILoggerProvider` adapter (Phase 09) so anything routed through `Microsoft.Extensions.Logging` is captured.
- **R2**: Storage budget feels too tight on Blazor — addressed in Phase 12 with explicit per-platform defaults and documented overrides.

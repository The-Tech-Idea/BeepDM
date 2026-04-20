# Phase 13 — DevEx, Testing, Rollout & Docs

## Objective

Close out the program with the developer-experience tooling, test doubles, performance harness, sample apps, runbook, and the public docs that turn this into a production-grade feature operators can confidently switch on.

## Dependencies

- Phases 01–12 in place.

## Scope

- **In**: Test sinks, fault injection, perf harness, sample apps, runbook, docs.
- **Out**: New runtime features.

## Target files

```
Services/Telemetry/Sinks/Test/
  RecordingSink.cs                    // partial: .Core, .Query
  RecordingSink.Core.cs
  RecordingSink.Query.cs
  FlakySink.cs
  SlowSink.cs
  FullDiskSink.cs

Services/Telemetry/Diagnostics/
  PerfHarness.cs                      // standalone CLI hook

Services/Examples/
  LoggingDesktopExample.cs
  LoggingWebApiExample.cs
  LoggingBlazorExample.cs
  AuditFormsExample.cs
  AuditDistributedExample.cs

Services/Plans/
  RUNBOOK.md
  CHANGELOG.md
```

Plus updates to:
- `Services/README.md` — adds "Logging" and "Audit" sections with quick-start.
- `Services/IMPLEMENTATION_SUMMARY.md` — adds delivered-features matrix.
- `Services/MIGRATION.md` — adds "Adopting BeepLog and BeepAudit" walkthrough.

## Design notes

### `RecordingSink`

In-memory sink with rich query helpers for tests:
- `IReadOnlyList<TelemetryEnvelope> All`.
- `IEnumerable<TelemetryEnvelope> WithCategory(string)`.
- `IEnumerable<AuditEvent> Audit(string source = null)`.
- `void Clear()`.

### Fault injection sinks

| Sink | Behavior |
|---|---|
| `FlakySink(failEvery: 7)`        | Throws every Nth `WriteBatchAsync`. |
| `SlowSink(latency: 250ms)`       | Awaits before completing each write. |
| `FullDiskSink(maxBytes: 1 MB)`   | Throws `IOException` after N bytes written. |

These verify the pipeline survives misbehaving sinks (no producer exceptions for logs; precise escalation for audit).

### `PerfHarness`

A tiny CLI / test entry that fires X envelopes/sec into the pipeline and reports queue depth, drop counts, and end-to-end latency to a `MemorySink`. Targets:

- Desktop:  ≥ 10k logs/sec sustained, ≥ 2k audit/sec sustained.
- Web:      ≥ 5k logs/sec under ASP.NET load.
- Blazor:   ≥ 200 logs/sec to IndexedDB.
- MAUI:     ≥ 1k logs/sec on a mid-range device.

### Sample apps

Each example is a self-contained file under `Services/Examples/` showing:
- DI registration with `AddBeepLoggingFor*` and `AddBeepAuditFor*`.
- A redaction example.
- A correlation scope example.
- Periodic flush + clean shutdown.

### `RUNBOOK.md`

Operator-facing playbook:

1. **Enable / disable** at runtime.
2. **Adjust budgets** safely (without losing audit history).
3. **Verify a chain** end to end (`IntegrityVerifier`).
4. **Recover** from a corrupt audit chain segment (export → quarantine → re-anchor).
5. **GDPR purge** — step by step including operator confirmation.
6. **Rotate the chain secret** without breaking verification of older segments.
7. **Drain & shut down** cleanly (CI/CD redeploys).
8. **Common errors** + fixes.

### `CHANGELOG.md`

Per-phase deltas; aligned with the MASTER tracker so each `[x]` flips here when a phase ships.

## Implementation steps

1. Add the test sinks (one class per file).
2. Add `PerfHarness` and capture baseline numbers per platform.
3. Author the five sample apps.
4. Write `RUNBOOK.md` with copy-paste commands and verified output.
5. Write `CHANGELOG.md` baseline.
6. Update `Services/README.md`, `IMPLEMENTATION_SUMMARY.md`, `MIGRATION.md`.
7. Final cross-check: every TODO in `MASTER-TODO-TRACKER.md` resolved.

## TODO checklist

- [ ] P13-01 `RecordingSink.{Core,Query}.cs`, `FlakySink.cs`, `SlowSink.cs`, `FullDiskSink.cs`.
- [ ] P13-02 `PerfHarness.cs` and recorded baseline.
- [ ] P13-03 Five sample apps under `Services/Examples/`.
- [ ] P13-04 `RUNBOOK.md`.
- [ ] P13-05 `CHANGELOG.md`.
- [ ] P13-06 Update `Services/README.md`, `IMPLEMENTATION_SUMMARY.md`, `MIGRATION.md`.
- [ ] P13-07 Verify every cross-cutting acceptance gate (G-01..G-06) in the MASTER tracker.

## Verification

- All sample apps build and run on their target hosts.
- `PerfHarness` meets or exceeds the per-platform targets above.
- `RUNBOOK.md` walkthroughs reproduce on a clean machine.
- `MASTER-TODO-TRACKER.md` is fully `[x]` and acceptance gates G-01..G-06 are checked.

## Risks

- **R1**: Sample apps drift over time. Mitigation: include them in the build; CI compiles them.
- **R2**: Perf targets may be host-dependent. Mitigation: targets are *floor* values on documented reference hardware; per-host overrides are allowed in the runbook.

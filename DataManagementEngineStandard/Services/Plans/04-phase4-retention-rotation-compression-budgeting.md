# Phase 04 — Retention, Rotation, Compression & Storage Budget

## Objective

Make the on-disk footprint **bounded and predictable** on every platform. Add rotation policy hooks, gzip-on-rotate, retention sweeping, and a hard `StorageBudget` that no log/audit data is allowed to exceed.

## Dependencies

- Phase 03 sinks emit `Rolled` events.

## Scope

- **In**: Rotation/retention policies, sweeper job, gzip compression, budget enforcer.
- **Out**: Encrypted-at-rest (v2), remote shipping (v2).

## Target files

```
Services/Telemetry/Retention/
  RotationPolicy.cs                  // size / time / event-count
  RetentionPolicy.cs                 // days + max files
  StorageBudget.cs
  IBudgetEnforcer.cs
  DefaultBudgetEnforcer.cs           // partial: .Core, .Sweep, .Compress
  DefaultBudgetEnforcer.Core.cs
  DefaultBudgetEnforcer.Sweep.cs
  DefaultBudgetEnforcer.Compress.cs
  RetentionSweeperHostedService.cs   // optional, opt-in
```

## Design notes

### Policies (data-only POCOs)

```csharp
public sealed class RotationPolicy
{
    public long      MaxFileBytes        { get; set; } = 5 * 1024 * 1024;
    public TimeSpan? RollInterval        { get; set; } = TimeSpan.FromHours(24);
    public int?      MaxEventsPerFile    { get; set; }
}

public sealed class RetentionPolicy
{
    public int   MaxFiles      { get; set; } = 30;
    public int   MaxAgeDays    { get; set; } = 30;
}

public sealed class StorageBudget
{
    public long  MaxTotalBytes { get; set; } = 50L * 1024 * 1024;
    public bool  CompressOnRotate { get; set; } = true;
    public BudgetBreachAction OnBreach { get; set; } = BudgetBreachAction.DeleteOldest;
}
```

### Sweeper

`RetentionSweeperHostedService` runs every `SweepInterval` (default 5 min) and:

1. Lists files in the sink's directory matching the sink's pattern.
2. Computes total bytes.
3. Applies, in order:
   a. `MaxAgeDays` cutoff (delete older).
   b. `MaxFiles` cap (delete oldest beyond cap).
   c. `MaxTotalBytes` cap (delete oldest until under budget).
4. Compresses any rotated `.ndjson` files that don't yet have a `.gz` sibling.
5. Emits a `BudgetBreached` self-event via `PipelineMetrics` (Phase 11) when step (c) had to fire.

### Gzip-on-rotate

Triggered by the `Rolled(filePath)` event from `FileRollingSink`. Streams the source file through `GzipStream` to `{file}.gz`, then deletes the original. Works on every platform `System.IO.Compression` supports (i.e. all of them).

For audit: compression is allowed but the **hash chain remains verifiable** because the chain operates over the JSON payload, not over the file bytes (Phase 08 design).

### Budget breach actions

| Action | Behavior |
|---|---|
| `DeleteOldest` (default for logs) | Sweeper deletes oldest files until under budget. |
| `BlockNewWrites` (audit-strict)   | Pipeline switches audit mode to `FailFast`; producers receive an exception until budget recovers. |
| `EmitOnly` (telemetry/test)       | Just raise the event; do nothing. |

For audit, the operator *must* explicitly opt into `DeleteOldest` (not the default) because deleting old audit records may breach compliance.

## Implementation steps

1. Add policies + budget POCOs.
2. Add `IBudgetEnforcer` and the default impl partials.
3. Wire `FileRollingSink.Roll` to invoke the enforcer's compress hook.
4. Add `RetentionSweeperHostedService` (opt-in, not started unless `Enabled`).
5. Extend `BeepLoggingOptions` and `BeepAuditOptions` with `RotationPolicy`, `RetentionPolicy`, `StorageBudget`.
6. Tests: budget breach with 100 small files + small `MaxTotalBytes`; verify oldest deleted, newest preserved, hash chain still verifiable for audit.

## TODO checklist

- [ ] P04-01 `RotationPolicy.cs`, `RetentionPolicy.cs`, `StorageBudget.cs`.
- [ ] P04-02 `IBudgetEnforcer.cs` + `DefaultBudgetEnforcer.{Core,Sweep,Compress}.cs`.
- [ ] P04-03 Gzip-on-rotate hook from `FileRollingSink`.
- [ ] P04-04 `RetentionSweeperHostedService.cs`.
- [ ] P04-05 Options surface for both features.
- [ ] P04-06 Tests: budget breach, retention age, max files, gzip compress.

## Verification

- 8-hour soak with 100 events/sec stays under configured `MaxTotalBytes` ± one file.
- After process restart, the sweeper continues to honor budget against pre-existing files.
- Gzipped audit files still verify under `IntegrityVerifier` (Phase 08).

## Risks

- **R1**: Sweeper deleting an in-use file. Mitigation: enforcer skips files that fail to open with `FileShare.Read` exclusive probe.
- **R2**: Compression CPU spike on low-power devices. Mitigation: option to disable `CompressOnRotate` per sink, especially on MAUI/Blazor.
- **R3**: Audit deletion under `DeleteOldest` may violate policy. Mitigation: default audit budget action is `BlockNewWrites`; doc clearly warns operators before they switch.

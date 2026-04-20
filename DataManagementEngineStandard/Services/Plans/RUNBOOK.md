# Beep Logging & Audit — Operator Runbook

This runbook is the **single playbook** operators follow once `BeepLog`
and `BeepAudit` are deployed. Every section is copy-paste ready and
includes verified output so it is obvious whether the step succeeded.

> **Conventions**
> - Code references use `IBeepLog`, `IBeepAudit`, and the
>   `Services/Examples/` files — they are the canonical demos.
> - File paths under `LocalApplicationData` are resolved by
>   `PlatformPaths` (see `Services/Telemetry/PlatformPaths.cs`).
> - The audit chain identifier is `default` unless the host overrides
>   `BeepAuditOptions.HashChainId`.

---

## 1. Enable / disable at runtime

The features are off by default. Turn them on in the host's
`Program.cs` using the platform extension that matches the host:

| Host | Logging method | Audit method |
|---|---|---|
| Console / WinForms / WPF | `AddBeepLoggingForDesktop` | `AddBeepAuditForDesktop` |
| ASP.NET Core | `AddBeepLoggingForWeb(env.ContentRootPath, "MyApp")` | `AddBeepAuditForWeb(env.ContentRootPath, "MyApp")` |
| Blazor WebAssembly | `AddBeepLoggingForBlazor(bridge)` | `AddBeepAuditForBlazor(bridge)` |
| MAUI | `AddBeepLoggingForMaui(() => FileSystem.AppDataDirectory)` | `AddBeepAuditForMaui(() => FileSystem.AppDataDirectory)` |

Disable at any time with `opt.Enabled = false` in the configure
callback. With `Enabled = false` the registration resolves to the
`Null*` implementation and **no pipeline is built**, so there is no
queue, no sinks, and no background work.

---

## 2. Adjust budgets safely

Budgets live in `BeepLoggingOptions.Budget` and `BeepAuditOptions.Budget`.

```csharp
services.AddBeepAuditForDesktop("MyApp", opt =>
{
    opt.Budget = new StorageBudget
    {
        MaxTotalBytes = 1L * 1024 * 1024 * 1024, // 1 GiB
        OnBreach      = BudgetBreachAction.BlockNewWrites,
        CompressOnRotate = true
    };
});
```

Rules of thumb:

- **Logs** use `BudgetBreachAction.DeleteOldest` — losing log lines is
  acceptable to keep the disk healthy.
- **Audit** uses `BudgetBreachAction.BlockNewWrites` — the producer
  blocks until you free space, because audit is lossless by policy.
- Increase audit `MaxTotalBytes` rather than switching to
  `DeleteOldest`. **Never** switch audit to `DeleteOldest` while a chain
  is in use — it severs sequence continuity and the integrity verifier
  will report a divergence.

---

## 3. Verify the audit chain end to end

Programmatic:

```csharp
IBeepAudit audit = sp.GetRequiredService<IBeepAudit>();
bool ok = await audit.VerifyIntegrityAsync(ct);
```

The call walks every chain segment, recomputes each event's HMAC, and
compares it to the persisted `Hash`. Internally this also bumps
`PipelineMetrics.ChainVerifiedTotal` and (on mismatch)
`ChainDivergenceTotal`.

**Expected output**: `ok == true` and the latest metrics snapshot shows
`chain_divergence_total = 0`. Any non-zero divergence value means the
on-disk chain has been mutated outside the pipeline (or a key change
was applied without re-anchoring — see step 6).

---

## 4. Recover from a corrupt chain segment

When `VerifyIntegrityAsync` returns `false` or the snapshot shows
`chain_divergence_total > 0`:

1. **Stop new writes**. Set `opt.Enabled = false` for audit OR pause
   the host. The pipeline drains the queue and disposes sinks cleanly.
2. **Export** the suspect range using `AuditExporter.ExportAsync`. Pass
   the affected `ChainId` and a from/to window. The exporter writes an
   `ExportManifest` that includes a signed digest.
3. **Quarantine** the original NDJSON / SQLite files. Move them out of
   `PlatformPaths.AuditDir` into a forensics folder.
4. **Re-anchor** the chain. Call `HashChainSigner.Reseal(chainId)` —
   this recomputes the chain from the surviving events and writes a
   fresh anchor record. The genesis sequence stays at 0.
5. **Re-enable** audit. New events extend the new anchor; the old
   exported segment remains independently verifiable from its manifest.

---

## 5. GDPR purge — step by step

```csharp
GdprPurgeService gdpr = sp.GetRequiredService<GdprPurgeService>();

PurgeImpact preview = await gdpr.PreviewByUserAsync("u-42", ct);
Console.WriteLine($"Will delete {preview.EventCount} events across "
                + $"{preview.ChainCount} chains.");

string token = ConfirmTokenPurgePolicy.IssueToken(preview);
// ↑ operator copies the token into a follow-up call to confirm intent.

GdprPurgeReport report = await gdpr.PurgeByUserAsync("u-42", token, ct);
Console.WriteLine(report.ToSummary());
```

What happens under the covers:

1. The query engine selects every event whose `UserId == "u-42"`.
2. Each affected chain is **re-signed** so verification still passes
   after deletion.
3. A synthetic `Operation = "GDPR.Purge"` audit event is appended to
   each touched chain so the deletion itself is auditable.
4. `Snapshot()` shows `chain_signed_total` increase by the number of
   re-signs and `chain_divergence_total` remains 0.

---

## 6. Rotate the chain secret

The HMAC key is supplied by `IKeyMaterialProvider`. To rotate:

1. **Add** the new key as the active key in your provider; keep the old
   key marked as a verifier-only key.
2. **Re-anchor** every active chain via `HashChainSigner.Reseal(chainId)`.
   Reseal uses the active key for the new anchor; older segments still
   verify because the verifier walks the keyring.
3. **Verify** with `audit.VerifyIntegrityAsync(ct)` — it must return
   `true`. If it does, you can decommission the old key after the
   retention window for the older segments has elapsed.

Never delete an old key while events signed by that key are still
within the retention window — verification will start failing for the
unrelated segment.

---

## 7. Drain & shut down cleanly (CI/CD redeploys)

```csharp
await provider.DisposeAsync();
```

Disposing the DI container drains the queue, awaits each sink's
`FlushAsync`, and disposes every sink. The desktop, web, MAUI, and
Blazor presets all rely on this single shutdown contract — no host
needs custom plumbing.

In ASP.NET Core, the framework calls `IHost.StopAsync` which in turn
disposes the root provider; in MAUI the lifecycle hook
`OnStop`/`OnDestroy` should call `await Services.DisposeAsync()` on
the root provider before exit. Failing to dispose drops in-flight
log batches; audit batches block at the queue boundary and will only
release on cancellation.

---

## 8. Common errors + fixes

| Symptom | Likely cause | Fix |
|---|---|---|
| `chain_divergence_total > 0` | On-disk audit was mutated outside the pipeline, or a key was rotated without reseal. | Run step 4 (recover from corrupt segment) and/or step 6 (rotate secret). |
| `dropped_queue_full_total` rising on audit pipeline | Audit queue is at capacity and `BackpressureMode = Block`. Producer thread is being throttled. | Increase `QueueCapacity`, add a faster sink, or move audit-heavy work onto a background task. |
| `sink_errors_total` rising on file sink | Disk is full or the directory was deleted out from under the sink. Sink will be marked unhealthy. | Reclaim disk via the retention sweeper / external cleanup; the sink self-heals on the next successful write. |
| Blazor sample never persists | Host did not register `IIndexedDbBridge`. | Register the bridge before calling `AddBeepLoggingForBlazor` / `AddBeepAuditForBlazor`. |
| MAUI sample throws `DirectoryNotFoundException` | The `Func<string>` for `AppDataDirectory` returned an unwritable path on a platform that requires a per-user directory. | Pass `() => FileSystem.AppDataDirectory` from the host so MAUI resolves the correct per-platform location. |
| `VerifyIntegrityAsync` returns `false` immediately after a manual edit of `audit.ndjson` | Expected — every edit outside the pipeline severs the chain. | Restore from backup or accept the divergence and reseal (step 4). |

---

## 9. Self-observability checklist

The pipeline emits self-diagnostic events under
`BeepTelemetry.Self.*`. Subscribe to your `RecordingSink` (test) or
inspect any sink that captures self events:

```csharp
MetricsSnapshot snap = pipeline.Metrics.Snapshot();
Console.WriteLine(MetricsSnapshotRenderer.Render(snap, MetricsSnapshotFormat.Text));
```

Healthy steady state on a desktop host:

```
pipeline=logging healthy=true queue=0/10000 mode=DropOldest
log_enqueued_total=N audit_enqueued_total=0
dropped_queue_full_total=0 sink_errors_total=0
chain_signed_total=0 chain_divergence_total=0
```

Anything outside this baseline (rising `dropped_*` counters,
`healthy=false`, non-zero `chain_divergence_total`) is the trigger to
work through the matching section above.

# Phase 10 — Query, Export, Purge & Compliance

## Objective

Make the recorded data **useful and compliant**. Add a unified query API (`IAuditQuery` + a logs query helper), export to CSV / JSON / NDJSON with a signed manifest, and GDPR-style purge that re-seals the audit chain.

## Dependencies

- Phase 03 (`SqliteSink` provides queryable backing for production).
- Phase 08 (chain re-seal).

## Scope

- **In**: Query API, exporters, purge by user/entity, signed export manifest.
- **Out**: Hosted UI / dashboards (those live in `Beep.Winform` and consume this API).

## Target files

```
Services/Audit/Query/
  AuditQuery.cs                       // builder; partial: .Core, .Filters
  AuditQuery.Core.cs
  AuditQuery.Filters.cs
  IAuditQueryEngine.cs
  SqliteAuditQueryEngine.cs
  FileScanAuditQueryEngine.cs

Services/Audit/Export/
  AuditExporter.cs                    // partial: .Core, .Csv, .Json, .Ndjson
  AuditExporter.Core.cs
  AuditExporter.Csv.cs
  AuditExporter.Json.cs
  AuditExporter.Ndjson.cs
  ExportManifest.cs
  ManifestSigner.cs

Services/Audit/Purge/
  IPurgePolicy.cs
  GdprPurgeService.cs                 // partial: .Core, .User, .Entity, .ResealChain
  GdprPurgeService.Core.cs
  GdprPurgeService.User.cs
  GdprPurgeService.Entity.cs
  GdprPurgeService.ResealChain.cs

Services/Logging/Query/
  ILogQueryEngine.cs
  SqliteLogQueryEngine.cs
  FileScanLogQueryEngine.cs
```

## Design notes

### `AuditQuery` builder

```csharp
public sealed partial class AuditQuery
{
    public AuditQuery Category(AuditCategory cat);
    public AuditQuery Operation(string op);
    public AuditQuery User(string userId);
    public AuditQuery Tenant(string tenant);
    public AuditQuery Entity(string entityName, string recordKey = null);
    public AuditQuery Source(string source);
    public AuditQuery Outcome(AuditOutcome outcome);
    public AuditQuery Between(DateTime fromUtc, DateTime toUtc);
    public AuditQuery WithProperty(string key, object value);
    public AuditQuery OrderBy(string field, bool desc = true);
    public AuditQuery Take(int max);
}
```

### Engines

- `SqliteAuditQueryEngine` — translates the builder to parameterized SQL over `telemetry` table (Phase 03).
- `FileScanAuditQueryEngine` — falls back to streaming NDJSON files if no SQLite sink is configured. Bounded by `Take` and the date range.

`IBeepAudit.QueryAsync` is implemented by composing whichever engine matches the configured sink set.

### Exporters

- CSV — flat shape with one row per `FieldChange`; reuses the proven format from `AuditManager.ExportToCsvAsync` for backward compatibility.
- JSON — `WriteIndented = true`; full envelope tree.
- NDJSON — one envelope per line; suited for downstream ingestion.

### Signed manifest

For every export, write an `ExportManifest`:

```json
{
  "exportId": "ulid",
  "fromUtc": "...", "toUtc": "...",
  "filter": { "category": "DataAccess", "user": "u123" },
  "recordCount": 12345,
  "sourceFiles": ["audit-20260101.ndjson.gz", "..."],
  "sha256": "...",
  "signature": "HMAC-SHA256 over the canonical manifest"
}
```

Signing uses the same `IKeyMaterialProvider` as the chain (Phase 08). The manifest is the audit-grade proof that the export hasn't been tampered with.

### GDPR purge

Two operations:

| Method | Effect |
|---|---|
| `PurgeByUserAsync(userId)` | Removes any audit event whose `UserId == userId`, then re-seals every affected chain segment. Writes a single `Purge` event (in `purge` chain) recording the operator + count. |
| `PurgeByEntityAsync(entityName, recordKey)` | Same, scoped to a single entity/record pair. |

Re-seal pseudo-code:

```csharp
foreach (var chain in affectedChains)
{
    var prev = chain.AnchorBeforeFirstRemoval;
    foreach (var rec in chain.RecordsAfterFirstRemoval)
    {
        rec.PrevHash = prev;
        rec.Hash     = signer.Sign(rec);
        prev         = rec.Hash;
    }
    anchorStore.Update(chain.Id, last: prev, sequence: ...);
}
```

All file rewrites use `tmp + fsync + rename` to be crash-safe.

### Logs query

`ILogQueryEngine` mirrors the audit one but for `TelemetryEnvelope` of `Kind = Log`. Only available when `SqliteSink` is configured (file scan is best-effort and not part of the v1 contract).

## Implementation steps

1. Add `AuditQuery` builder partials.
2. Add `IAuditQueryEngine` + SQLite + file-scan engines.
3. Wire `BeepAudit.QueryAsync` to the matching engine.
4. Add `AuditExporter` partials and `ExportManifest` + `ManifestSigner`.
5. Add `GdprPurgeService` partials with re-seal.
6. Add log query interfaces and SQLite engine.
7. Tests: query by user/entity/date; export+verify manifest; purge user → re-verify chain.

## TODO checklist

- [x] P10-01 `AuditQuery.{Core,Filters}.cs`.
- [x] P10-02 `IAuditQueryEngine.cs` + `SqliteAuditQueryEngine.cs` + `FileScanAuditQueryEngine.cs` + `CompositeAuditQueryEngine.cs`.
- [x] P10-03 `AuditExporter.{Core,Csv,Json,Ndjson}.cs` + `ExportManifest.cs` + `ManifestSigner.cs` + `AuditExportResult.cs` + `ExportFormat.cs`.
- [x] P10-04 `IPurgePolicy.cs` + `ConfirmTokenPurgePolicy.cs` + `GdprPurgeService.{Core,User,Entity,ResealChain}.cs` + `IAuditPurgeStore.cs` + `PurgeImpact.cs` + `HashChainSigner.Reseal.cs`.
- [x] P10-05 `ILogQueryEngine.cs` + `LogQuery.cs` + `LogRecord` + `SqliteLogQueryEngine.cs` + `FileScanLogQueryEngine.cs` + `CompositeLogQueryEngine.cs` + `NdjsonLogDeserializer.cs` + `SqliteSink.QueryLog.cs`.
- [ ] P10-06 Tests for query, export+verify, purge+re-seal (deferred to Phase 13).
- [x] DI: extended `BeepServiceExtensions.Audit.cs` (query engine, purge service, integrity verifier, exporter, manifest signer) and `BeepServiceExtensions.Logging.cs` (log query engine).
- [x] Build + lint clean on net8.0 / net9.0 / net10.0.

## Verification

- Query for `user='u123'` over 1 M events returns < 500 ms with SQLite sink.
- Export of 100k events produces a valid manifest whose signature verifies.
- After `PurgeByUserAsync`, `IntegrityVerifier` (Phase 08) reports zero divergences and a `Purge` event is present in the `purge` chain.
- File-scan engine handles gzipped audit files transparently.

## Risks

- **R1**: Re-seal failure mid-flight. Mitigation: per-file `tmp + rename` is atomic; in case of crash, the verifier flags divergence and the runbook (Phase 13) describes recovery from the prior anchor.
- **R2**: Performance of file-scan engine on multi-GB log dirs. Mitigation: documented as fallback only; recommend SQLite sink in production.
- **R3**: Operator deletes the wrong user. Mitigation: purge requires a confirmation token + writes the operator id to the `purge` chain.

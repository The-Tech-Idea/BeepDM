# FormsManager — Audit

This document covers the audit subsystem: per-record and per-field change tracking, audit configuration, audit log query, and audit log export / purge.

## Concepts

- **`AuditManager`** — the engine-side orchestrator for audit.
- **`IAuditStore`** — the pluggable backing store. Two implementations: `FileAuditStore` (file-backed) and `InMemoryAuditStore` (in-memory only).
- **`AuditConfiguration`** — the per-engine configuration: enabled, audited blocks, audited fields, retention.
- **`AuditEntry`** — a single audit record: who, when, what block, what operation (insert/update/delete), the record ID.
- **`AuditFieldChange`** — a single field change: field name, old value, new value.

## Enabling audit

```csharp
manager.ConfigureAudit(config =>
{
    config.Enabled = true;
    config.AuditedBlocks.Add("ORDERS");
    config.AuditedFields.Add("Salary");      // optional: only audit specific fields
    config.RetentionDays = 90;              // 0 = forever
    config.Store = AuditStoreKind.File;     // or InMemory
    config.FilePath = @"C:\Logs\audit.log";
});

manager.SetAuditUser("fahad");
```

`ConfigureAudit` accepts a `Action<AuditConfiguration>` so you can configure the manager fluently. The configuration is applied immediately.

`SetAuditUser` sets the user that the audit log will attribute changes to. Set this on login / on form open.

## What gets audited

When audit is enabled, every **insert / update / delete** on an audited block produces an `AuditEntry`. If `AuditedFields` is non-empty, only those fields' changes are recorded as `AuditFieldChange`s; the rest of the record change is recorded as a single entry with no field-level detail.

**Reads (queries, navigation) are NOT audited.** Audit is for DML only. If you need to log reads, use a separate observability layer (e.g. the `IDMEEditor.AddLogMessage` family).

## Audit log queries

### `GetAuditLog(blockName?)`

```csharp
// All audit entries (across all audited blocks)
var all = manager.GetAuditLog();

// Only entries for a specific block
var orders = manager.GetAuditLog("ORDERS");
```

Returns `IReadOnlyList<AuditEntry>`. Each entry has:

- `Timestamp` (UTC).
- `User` (from `SetAuditUser`).
- `BlockName`.
- `Operation` (Insert / Update / Delete).
- `RecordId` (the primary key of the affected record, if available).
- `FieldChanges` (the field-level changes for Update operations).

### `GetFieldHistory(blockName, fieldName)`

```csharp
var salaryHistory = manager.GetFieldHistory("ORDERS", "Salary");
// Returns all changes to the Salary field on any order, in chronological order
```

This is the "who changed this field on this block over time" query. Useful for compliance / audit reports.

## Audit log export

```csharp
await manager.ExportAuditToCsvAsync(@"C:\Reports\audit.csv");
await manager.ExportAuditToJsonAsync(@"C:\Reports\audit.json");

// Optionally filter by block at export time
await manager.ExportAuditToCsvAsync(@"C:\Reports\orders-audit.csv", blockName: "ORDERS");
```

Export is **synchronous with the file I/O** (the `await` is for the async signature, not for offloading). The file is written in append mode by default.

## Audit log purge

```csharp
// Delete audit entries older than 90 days
manager.PurgeAudit(olderThanDays: 90);

// Delete ALL audit entries
manager.ClearAudit();
```

`PurgeAudit` is **time-based** — it deletes entries with `Timestamp < DateTime.UtcNow.AddDays(-olderThanDays)`. `ClearAudit` deletes everything.

## Audit storage

### `FileAuditStore` (default if you set a `FilePath`)

The file is a **line-delimited JSON** file. Each line is one `AuditEntry` or one `AuditFieldChange` (grouped by entry). Append-only. The file can grow indefinitely unless you call `PurgeAudit`.

### `InMemoryAuditStore`

A `ConcurrentQueue<AuditEntry>` (or similar). Lost on process restart. Useful for tests and short-lived sessions.

### Pluggability

You can implement `IAuditStore` to back audit with a database table, a SIEM feed, or any other sink. The default is `InMemoryAuditStore` if no path is set; `FileAuditStore` if a path is set.

## Audit events

The orchestrator does NOT have a dedicated `OnAuditEntry` event. The audit is a **sink** (the `IAuditStore` receives entries), not a **source** (which would raise events). The `AuditManager` writes to the store on every DML operation.

A host that wants to observe audit activity in real time can wrap the `IAuditStore` with a logging or event-raising wrapper.

## Field-level vs record-level audit

`AuditConfiguration` has two settings:

- `AuditedBlocks` — blocks to audit.
- `AuditedFields` — fields to audit at the field level (per-block, comma-separated, or a separate config).

If `AuditedFields` is empty, the audit is **record-level only** — every DML produces one `AuditEntry` with no field-level detail. If `AuditedFields` is set, the audit is **field-level** — every changed field on an updated record produces one `AuditFieldChange`.

## Audit on commit vs on every change

The default is **on commit**. The audit entry is recorded when `CommitFormAsync` succeeds, not when the change is made in the UoW. If the user rolls back, the audit entry is NOT recorded.

If you want audit-on-every-change (Oracle Forms behavior pre-6i), set `AuditConfiguration.AuditOnEveryChange = true`. The entry is recorded as soon as the field value is set in the UoW. Rollbacks will leave orphan audit entries (they record the change but the data wasn't committed).

## `ApplyAuditDefaults` (record pre-population)

`FormsManager.ApplyAuditDefaults(record, currentUser)` sets the `CreatedBy` / `CreatedOn` / `ModifiedBy` / `ModifiedOn` fields on a record before insert. This is a **pre-population** helper, not an audit write. The actual audit is recorded on commit.

## Notes for callers

- The audit log is **best-effort**. If writing to the file store fails (disk full, permission denied), the failure is logged to `_errorLog` but the operation continues. Audit is not a transactional part of the commit.
- The audit entry's `RecordId` is captured at audit time. If the record's primary key is auto-generated by the datasource (and the audit fires before the refresh), the `RecordId` may be `null` or a placeholder.
- Audit is **per-`FormsManager` instance**. Two instances in the same process have independent audit stores. For global audit, implement a custom `IAuditStore` that aggregates.
- The `IAuditStore` is **append-only** at the interface level. There is no `Update` or `Delete` method on the store; only `Append`, `Query`, `Purge`, `Clear`. This makes it safe to back audit with an append-only log (e.g. Loki, Kafka).
- The `AuditEntry.Operation` is one of three values: `Insert`, `Update`, `Delete`. Other operations (commit, rollback, query) are NOT recorded.

## See also

- [`architecture.md`](../architecture.md) — where `AuditManager` sits in the helper layer.
- [`security.md`](security.md) — security violations (a different audit concern).
- [`triggers.md`](triggers.md) — `OnPostInsert` / `OnPostUpdate` / `OnPostDelete` events (orthogonal to audit).
- [`ORACLE-FORMS-MAPPING.md`](../ORACLE-FORMS-MAPPING.md) section 16 — the audit mapping.

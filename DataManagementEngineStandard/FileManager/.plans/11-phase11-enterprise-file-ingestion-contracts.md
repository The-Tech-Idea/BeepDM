# Phase 11 — Enterprise File Ingestion Contracts

| Attribute      | Value                                    |
|----------------|------------------------------------------|
| Phase          | 11                                       |
| Status         | planned                                  |
| Priority       | Critical                                 |
| Dependencies   | Phase 1 (reader contracts), Phase 4 (streaming) |
| Est. Effort    | 5 days                                   |

---

## 1. Goal

Establish a first-class **ingestion contract layer** over `CSVDataSource` that every enterprise file import must honour.  
This layer makes ingestion idempotent, observable, and rollback-safe regardless of file size, source system, or consumer.

---

## 2. Motivation

| Current state | Enterprise requirement |
|---------------|------------------------|
| Every call to `GetEntity` re-reads the file from scratch | Idempotent: re-running the same import must yield the same result |
| No deduplication of already-ingested files | File fingerprint check — skip if already processed |
| Partial failures silently lose rows | Offset tracking — resume from the last committed checkpoint |
| No context on where data came from | Provenance envelope attached to every row or batch |
| No formal lifecycle state | State machine: Pending → Validating → Ingesting → Complete / Failed / Quarantine |

---

## 3. Contracts to Define

### 3.1 `IFileIngestionDescriptor`

```csharp
namespace TheTechIdea.Beep.FileManager.Contracts
{
    /// <summary>
    /// Immutable description of a single file ingestion job.
    /// Pass this into the ingestion pipeline in place of raw file paths.
    /// </summary>
    public interface IFileIngestionDescriptor
    {
        /// <summary>Globally unique job ID (GUID or ULID).</summary>
        string JobId { get; }

        /// <summary>SHA-256 hex of the file content at time of scheduling.</summary>
        string FileChecksum { get; }

        /// <summary>Absolute path to the source file.</summary>
        string FilePath { get; }

        /// <summary>UTC timestamp when the job was created.</summary>
        DateTimeOffset ScheduledAt { get; }

        /// <summary>
        /// Name of the target entity / table that will receive rows.
        /// </summary>
        string TargetEntityName { get; }

        /// <summary>Logical source system label (e.g. "CRM-Export", "ERP-Daily").</summary>
        string SourceSystem { get; }

        /// <summary>Optional partition or tenant key.</summary>
        string TenantId { get; }

        /// <summary>Arbitrary tags for routing, filtering, or governance.</summary>
        IReadOnlyDictionary<string, string> Tags { get; }
    }
}
```

### 3.2 `IIngestionStateStore`

Tracks lifecycle state keyed by `JobId`.  
Must be injectable (in-process SQLite, Redis, or SQL Server are all valid implementations).

```csharp
namespace TheTechIdea.Beep.FileManager.Contracts
{
    public enum IngestionState
    {
        Pending,
        Validating,
        Ingesting,
        Suspended,   // Checkpoint saved; can resume
        Complete,
        Failed,
        Quarantined  // File flagged for manual review
    }

    public interface IIngestionStateStore
    {
        /// <summary>Creates a new job entry. Throws if JobId already exists.</summary>
        Task CreateAsync(IFileIngestionDescriptor descriptor, CancellationToken ct = default);

        /// <summary>Transitions the job to a new state, optionally recording a message.</summary>
        Task TransitionAsync(string jobId, IngestionState newState, string message = null, CancellationToken ct = default);

        /// <summary>Records a row-level checkpoint so ingestion can resume.</summary>
        Task SaveCheckpointAsync(string jobId, long bytesRead, long rowsCommitted, CancellationToken ct = default);

        /// <summary>Reads the last saved checkpoint for a job (null if none).</summary>
        Task<IngestionCheckpoint> GetCheckpointAsync(string jobId, CancellationToken ct = default);

        /// <summary>Returns latest state snapshot for a job.</summary>
        Task<IngestionJobStatus> GetStatusAsync(string jobId, CancellationToken ct = default);

        /// <summary>
        /// Returns the JobId of any existing Complete job for this file checksum,
        /// enabling idempotency enforcement.
        /// </summary>
        Task<string> FindByChecksumAsync(string sha256Checksum, string targetEntityName, CancellationToken ct = default);
    }

    public sealed record IngestionCheckpoint(
        string JobId,
        long BytesRead,
        long RowsCommitted,
        DateTimeOffset SavedAt);

    public sealed record IngestionJobStatus(
        string JobId,
        IngestionState State,
        long RowsCommitted,
        long RowsRejected,
        DateTimeOffset LastUpdatedAt,
        string LastMessage);
}
```

### 3.3 `IFileProvenanceEnvelope`

Attached to every row or batch to record its origin.

```csharp
namespace TheTechIdea.Beep.FileManager.Contracts
{
    /// <summary>
    /// Provenance metadata attached by the ingestion pipeline.
    /// Stored alongside each ingested row (or batch header) for lineage.
    /// </summary>
    public interface IFileProvenanceEnvelope
    {
        string JobId { get; }
        string FileChecksum { get; }
        string SourceFilePath { get; }
        string SourceSystem { get; }
        long   SourceRowIndex { get; }   // 1-based line number in source file
        DateTimeOffset IngestedAt { get; }
    }
}
```

### 3.4 `IIdempotentFileIngester`

Top-level orchestrator that callers use instead of calling `CSVDataSource` directly.

```csharp
namespace TheTechIdea.Beep.FileManager.Contracts
{
    public interface IIdempotentFileIngester
    {
        /// <summary>
        /// Checks if the file described by <paramref name="descriptor"/> has already
        /// been successfully ingested.  Returns the prior JobId if so, null otherwise.
        /// </summary>
        Task<string> IsAlreadyIngestedAsync(
            IFileIngestionDescriptor descriptor,
            CancellationToken ct = default);

        /// <summary>
        /// Runs the ingestion pipeline for <paramref name="descriptor"/>.
        /// Idempotent: calling twice with the same file checksum is a no-op on the
        /// second call (returns the existing Complete job status).
        /// </summary>
        Task<IngestionJobStatus> IngestAsync(
            IFileIngestionDescriptor descriptor,
            IProgress<IngestionProgressArgs> progress = null,
            CancellationToken ct = default);

        /// <summary>
        /// Resumes a previously Suspended job from its last checkpoint.
        /// </summary>
        Task<IngestionJobStatus> ResumeAsync(
            string jobId,
            IProgress<IngestionProgressArgs> progress = null,
            CancellationToken ct = default);
    }

    public sealed record IngestionProgressArgs(
        string JobId,
        IngestionState State,
        long  RowsRead,
        long  RowsCommitted,
        long  RowsRejected,
        double ProgressPercent);
}
```

---

## 4. State Machine

```
                     ┌──────────────────────────────────────┐
                     │            INGESTION STATE MACHINE    │
                     └──────────────────────────────────────┘

  [start]
     │
     ▼
 Pending ──validate──► Validating
                            │ validation OK              │ validation failed
                            ▼                             ▼
                        Ingesting                     Quarantined
                            │ row processing
                       ┌────┴────────────────────────────┐
                       │ checkpoint saved                 │ error / cancellation
                       ▼                                  ▼
                   Suspended ─── resume ──► Ingesting  Failed
                       │
                       │ all rows committed
                       ▼
                   Complete
```

**Transition rules:**
- `Pending → Validating` : on StartAsync call
- `Validating → Ingesting` : header/schema checks pass
- `Validating → Quarantined` : schema mismatch, file corrupt, PII policy violation
- `Ingesting → Suspended` : CancellationToken signalled OR periodic checkpoint (every N rows)
- `Ingesting → Complete` : last row committed, counts reconcile
- `Ingesting → Failed` : unrecoverable IO or parse error
- `Suspended → Ingesting` : ResumeAsync called
- `Failed → Pending` : manual retry only (new JobId recommended)

---

## 5. Idempotency Algorithm

```
function IngestAsync(descriptor):
    checksum = descriptor.FileChecksum  (or compute SHA-256 if not pre-set)

    priorJobId = await stateStore.FindByChecksumAsync(checksum, descriptor.TargetEntityName)
    if priorJobId != null:
        return await stateStore.GetStatusAsync(priorJobId)   // no-op, return prior result

    await stateStore.CreateAsync(descriptor)
    await stateStore.TransitionAsync(descriptor.JobId, Validating)

    validationResult = await validator.ValidateAsync(descriptor)
    if validationResult.IsFailed:
        await stateStore.TransitionAsync(descriptor.JobId, Quarantined, validationResult.Reason)
        return GetStatusAsync(descriptor.JobId)

    await stateStore.TransitionAsync(descriptor.JobId, Ingesting)
    checkpoint = await stateStore.GetCheckpointAsync(descriptor.JobId)  // null for new jobs

    await IngestRowsAsync(descriptor, startOffset: checkpoint?.BytesRead ?? 0, ...)
    await stateStore.TransitionAsync(descriptor.JobId, Complete)
    return GetStatusAsync(descriptor.JobId)
```

---

## 6. File Fingerprinting

```csharp
/// <summary>
/// Computes a stable fingerprint for a file.
/// Uses SHA-256 of file content rather than modification time
/// because mod-time can be wrong after copy/transfer.
/// </summary>
public static async Task<string> ComputeChecksumAsync(string filePath, CancellationToken ct = default)
{
    using var sha = System.Security.Cryptography.SHA256.Create();
    await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                                            FileShare.Read, bufferSize: 81920, useAsync: true);
    var hashBytes = await sha.ComputeHashAsync(stream, ct);
    return Convert.ToHexString(hashBytes); // .NET 5+
}
```

**Caching rule:** Cache checksum in `IIngestionStateStore` — do NOT re-hash on every call.  
**Large-file optimisation:** For files > 100 MB, hash only the first 64 KB + last 64 KB + file length as a quick-dedup fingerprint; fall back to full hash for exact dedup when needed.

---

## 7. Row Envelope — Wiring into `CSVDataSource`

The `GetEntity` method should optionally accept an `IFileIngestionDescriptor` and attach provenance:

```csharp
// In CSVDataSource — new optional overload
public object GetEntity(string EntityName, List<AppFilter> filter,
                        IFileIngestionDescriptor descriptor = null,    // NEW
                        long startRowIndex = 0)                        // NEW (resume)
{
    // ... existing header parse ...
    // For each row returned:
    if (descriptor != null)
    {
        var envelope = new FileProvenanceEnvelope(
            descriptor.JobId,
            descriptor.FileChecksum,
            descriptor.FilePath,
            descriptor.SourceSystem,
            currentRowIndex,
            DateTimeOffset.UtcNow);
        row[ProvEnvelopeColumnName] = envelope; // or attach to a metadata bag
    }
    // ...
}
```

---

## 8. Acceptance Criteria

| # | Criterion                                                                                        | Test |
|---|--------------------------------------------------------------------------------------------------|------|
| 1 | Ingesting the same file twice (identical checksum) returns the first job's `Complete` status     | Unit |
| 2 | A failed mid-file ingestion can be resumed from the last checkpoint after fix                    | Integration |
| 3 | A file with a broken header transitions to `Quarantined`, not `Failed`                           | Unit |
| 4 | Every ingested row carries `JobId`, `FileChecksum`, `SourceRowIndex` in its provenance envelope  | Unit |
| 5 | `ComputeChecksumAsync` is deterministic for the same bytes regardless of OS or filesystem        | Unit |
| 6 | `IIngestionStateStore` has an in-memory reference implementation usable in unit tests            | Unit |

---

## 9. Deliverables

| Artifact | Location |
|----------|----------|
| `Contracts/IFileIngestionDescriptor.cs` | `FileManager/Contracts/` |
| `Contracts/IIngestionStateStore.cs` | `FileManager/Contracts/` |
| `Contracts/IFileProvenanceEnvelope.cs` | `FileManager/Contracts/` |
| `Contracts/IIdempotentFileIngester.cs` | `FileManager/Contracts/` |
| `Contracts/IngestionState.cs` (enum + record types) | `FileManager/Contracts/` |
| `InMemoryIngestionStateStore.cs` | `FileManager/Implementations/` |
| `FileChecksumHelper.cs` | `FileManager/Utilities/` |
| Unit tests | `tests/FileManager/IngestionContractTests.cs` |

---

## 10. Enterprise Standards Traceability

| Standard | Clause | Addressed by this phase |
|----------|--------|------------------------|
| ISO 8000-8 | Data Quality — provenance metadata | `IFileProvenanceEnvelope` |
| CDM / Data Mesh | Data product contract | `IFileIngestionDescriptor` |
| SOX / GDPR | Audit trail for data ingestion | `IIngestionStateStore` transition log |
| CIS Control 3 | Data protection | Idempotency prevents duplicate PII records |

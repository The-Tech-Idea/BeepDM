# Phase 13 — Resilience and Continuity

| Attribute      | Value                                      |
|----------------|--------------------------------------------|
| Phase          | 13                                         |
| Status         | planned                                    |
| Priority       | Critical                                   |
| Dependencies   | Phase 4 (streaming), Phase 11 (ingestion contracts), Phase 5 (error handling) |
| Est. Effort    | 5 days                                     |

---

## 1. Goal

Ensure that **no file ingestion job fails silently or loses work**.  
Large files must checkpoint their progress so ingestion can be resumed.  
Individual row errors must be isolated (dead-lettered), not abort the entire job.  
Transient failures must be retried with exponential backoff.  
File events must be detectable so pipelines can run on-arrival rather than on-poll.

---

## 2. Motivation

| Current state | Enterprise requirement |
|---------------|------------------------|
| `GetEntity` throws or returns empty list on any parse error | Row errors must be isolated; healthy rows must still be delivered |
| No checkpointing — restarts re-process the whole file | Resume from last committed checkpoint |
| No retry logic on transient IO errors | Retry with backoff on file-locked / network share unavailable |
| No dead-letter queue — bad rows vanish | Every rejected row quarantined in a dead-letter store with error detail |
| Must poll for new files | File-system event trigger for on-arrival ingestion |

---

## 3. Checkpoint / Resume Design

### 3.1 Checkpoint granularity

Checkpoints are stored into `IIngestionStateStore` (Phase 11).  
Two checkpoint modes:

| Mode | When checkpointed | Trade-off |
|------|-------------------|-----------|
| **Row-count** | Every N rows (default: 10 000) | Simple; small state |
| **Byte-offset** | Every M bytes (default: 50 MB) | Exact replay start; works for streaming reads |

Default: byte-offset mode, because CSV rows are variable-length and binary seek to offset is O(1).

### 3.2 Resume algorithm

```
function ResumeAsync(jobId):
    checkpoint = await stateStore.GetCheckpointAsync(jobId)
    descriptor = await stateStore.GetDescriptorAsync(jobId)

    // Verify file has not changed since checkpoint
    currentChecksum = await FileChecksumHelper.ComputeChecksumAsync(descriptor.FilePath)
    if currentChecksum != descriptor.FileChecksum:
        throw FileChangedAfterCheckpointException(jobId)    // must start fresh

    await stateStore.TransitionAsync(jobId, Ingesting, "Resuming from checkpoint")

    using stream = File.OpenRead(descriptor.FilePath)
    stream.Seek(checkpoint.BytesRead, SeekOrigin.Begin)     // skip already-committed bytes

    await IngestRowsAsync(descriptor, stream, startRowIndex: checkpoint.RowsCommitted)
```

### 3.3 Checkpoint frequency contract

```csharp
public sealed class CheckpointPolicy
{
    /// <summary>Save a checkpoint after this many rows are committed.</summary>
    public int RowsPerCheckpoint { get; init; } = 10_000;

    /// <summary>Save a checkpoint after this many bytes are read (takes priority if set).</summary>
    public long BytesPerCheckpoint { get; init; } = 50 * 1024 * 1024; // 50 MB

    /// <summary>
    /// If true, always save a final checkpoint after the last row,
    /// even if the batch threshold has not been reached.
    /// </summary>
    public bool CheckpointOnComplete { get; init; } = true;
}
```

---

## 4. Dead-Letter Row Store

### 4.1 Contract

```csharp
namespace TheTechIdea.Beep.FileManager.Resilience
{
    public interface IDeadLetterStore
    {
        /// <summary>
        /// Records a row that could not be processed.
        /// </summary>
        Task WriteAsync(DeadLetterEntry entry, CancellationToken ct = default);

        /// <summary>
        /// Returns all dead-letter entries for a given job.
        /// </summary>
        Task<IReadOnlyList<DeadLetterEntry>> GetByJobAsync(string jobId, CancellationToken ct = default);

        /// <summary>
        /// Returns a paginated view for monitoring dashboards.
        /// </summary>
        Task<IReadOnlyList<DeadLetterEntry>> GetRecentAsync(int limit = 100, CancellationToken ct = default);

        /// <summary>
        /// Marks an entry as manually resolved (e.g. re-processed after fix).
        /// </summary>
        Task ResolveAsync(string entryId, string resolvedBy, string notes, CancellationToken ct = default);
    }

    public sealed class DeadLetterEntry
    {
        public string Id { get; init; }              // GUID
        public string JobId { get; init; }
        public long   SourceRowIndex { get; init; }  // 1-based line number
        public string RawLine { get; init; }         // original CSV line text
        public string ErrorCategory { get; init; }   // ParseError | TypeConversionError | SchemaViolation | PolicyViolation
        public string ErrorMessage { get; init; }
        public string ColumnName { get; init; }      // which column caused the error (if known)
        public DateTimeOffset OccurredAt { get; init; }
        public bool IsResolved { get; init; }
        public string ResolvedBy { get; init; }
    }
}
```

### 4.2 Error category taxonomy

| Category | Example | Recoverable? |
|----------|---------|--------------|
| `ParseError` | Quoted field never closed | No — manual fix required |
| `TypeConversionError` | "abc" in an int column | Possibly — check source system |
| `SchemaViolation` | Wrong number of columns | Possibly — check for embedded delimiter |
| `PolicyViolation` | Unmasked PII in a restricted column | No — governance issue |
| `ConstraintViolation` | Null in a NOT-NULL target field | Possibly — upstream fix |
| `DuplicateKey` | Row with same primary key already exists | Possibly — deduplication logic |

### 4.3 Dead-letter file export

For environments without a database, provide a CSV dead-letter export:

```csharp
// IDeadLetterStore default implementation: write to a .deadletter.csv alongside the source file
// e.g. /data/customers.csv  →  /data/customers.20250101T120000Z.deadletter.csv
```

---

## 5. Retry Policy

### 5.1 Which errors should be retried?

| Error type | Retry? | Reason |
|------------|--------|--------|
| `FileNotFoundException` (file not yet arrived) | Yes, up to 3 times with 5s delay | Network share lag |
| `IOException` with file locked | Yes | Another process writing the file |
| `UnauthorizedAccessException` | No | Permission problem — alert ops |
| `OperationCanceledException` | No — save checkpoint, suspend job | Intentional cancellation |
| Parse / type errors | **No** — dead-letter the row | Retrying won't fix data quality |

### 5.2 Retry contract

```csharp
namespace TheTechIdea.Beep.FileManager.Resilience
{
    public sealed class FileRetryPolicy
    {
        public int MaxAttempts { get; init; } = 3;
        public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(2);
        public double BackoffMultiplier { get; init; } = 2.0;   // 2s, 4s, 8s
        public TimeSpan MaxDelay { get; init; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Returns true if the exception is a transient IO error worth retrying.
        /// </summary>
        public bool IsTransient(Exception ex) =>
            ex is IOException ||
            (ex is UnauthorizedAccessException uae && IsFileLockError(uae));

        private static bool IsFileLockError(Exception ex) =>
            ex.HResult is unchecked((int)0x80070020) or  // ERROR_SHARING_VIOLATION
                          unchecked((int)0x80070021);     // ERROR_LOCK_VIOLATION
    }
}
```

### 5.3 Retry wrapper

```csharp
public static async Task<T> ExecuteWithRetryAsync<T>(
    Func<CancellationToken, Task<T>> action,
    FileRetryPolicy policy,
    CancellationToken ct)
{
    var delay = policy.InitialDelay;
    for (int attempt = 1; ; attempt++)
    {
        try
        {
            return await action(ct);
        }
        catch (Exception ex) when (attempt < policy.MaxAttempts && policy.IsTransient(ex))
        {
            await Task.Delay(delay, ct);
            delay = TimeSpan.FromMilliseconds(
                Math.Min(delay.TotalMilliseconds * policy.BackoffMultiplier,
                         policy.MaxDelay.TotalMilliseconds));
        }
    }
}
```

---

## 6. File-System Event Trigger

### 6.1 Goal

Allow ingestion pipelines to react to file arrival rather than polling on a schedule.

### 6.2 Contract

```csharp
namespace TheTechIdea.Beep.FileManager.Resilience
{
    public interface IFileArrivalTrigger : IDisposable
    {
        /// <summary>
        /// Raised when a file matching <see cref="WatchPattern"/> is created or completely written
        /// in the watched directory.
        /// </summary>
        event EventHandler<FileArrivedEventArgs> FileArrived;

        string WatchDirectory { get; }
        string WatchPattern { get; }    // e.g. "*.csv", "export_*.txt"
        bool   IsWatching { get; }

        void Start();
        void Stop();
    }

    public sealed class FileArrivedEventArgs : EventArgs
    {
        public string FilePath { get; init; }
        public long   FileSizeBytes { get; init; }
        public DateTimeOffset ArrivedAt { get; init; }
    }
}
```

### 6.3 Implementation strategy

Use `System.IO.FileSystemWatcher` with a **stability gate**:
- On `Created` or `Changed` event, do NOT trigger immediately.
- Poll the file size at 500 ms intervals until size is stable for 2 consecutive polls — only then raise `FileArrived`.
- This prevents triggering on a partially-written file.

```csharp
public sealed class FileSystemWatcherTrigger : IFileArrivalTrigger
{
    // FileSystemWatcher + stability timer
    // Raises FileArrived only after file size is stable for _stabilityWindow
    private readonly TimeSpan _stabilityWindow = TimeSpan.FromSeconds(2);
    // ... implementation ...
}
```

---

## 7. Partial Failure Handling Matrix

```
For each row during IngestRowsAsync:

   TryParse(row)
       │
       ├── Success ──► Accumulate in commit buffer
       │                   │
       │                   └── Buffer full (commitBatchSize rows)
       │                           ↓
       │                       CommitBatch() ──► SaveCheckpoint()
       │
       └── ParseError / TypeError / PolicyViolation
               ↓
           deadLetterStore.WriteAsync(entry)
           Increment RejectedCount
           Continue to next row   ← NO abort
               ↓
           If RejectedCount > MaxRejectedFraction:
               ↓
               Suspend job + alert  ← quality gate
```

### 7.1 Quality gate (max rejection threshold)

```csharp
public sealed class IngestionQualityGate
{
    /// <summary>
    /// If the fraction of rejected rows exceeds this threshold, suspend the job.
    /// Default: 5% rejected rows triggers suspension.
    /// </summary>
    public double MaxRejectedFraction { get; init; } = 0.05;

    /// <summary>
    /// Minimum number of rows that must be read before applying the fraction check.
    /// Prevents early suspension on tiny files.
    /// </summary>
    public int MinRowsBeforeCheck { get; init; } = 100;
}
```

---

## 8. Acceptance Criteria

| # | Criterion | Test |
|---|-----------|------|
| 1 | Ingesting a 1M-row file, cancel at row 200K, resume — no rows duplicated or skipped | Integration |
| 2 | A file that changes between checkpoint and resume throws `FileChangedAfterCheckpointException` | Unit |
| 3 | A row with a type error is dead-lettered; the next row is still processed | Unit |
| 4 | Rejection rate > 5% on a 1000-row file → job suspended, not failed | Unit |
| 5 | `FileSystemWatcherTrigger` does not raise `FileArrived` until file write is complete | Integration |
| 6 | Transient `IOException` is retried up to MaxAttempts with exponential backoff | Unit |
| 7 | `IDeadLetterStore.ResolveAsync` marks entry resolved and it no longer appears in `GetRecentAsync` | Unit |

---

## 9. Deliverables

| Artifact | Location |
|----------|----------|
| `Resilience/CheckpointPolicy.cs` | `FileManager/Resilience/` |
| `Resilience/IDeadLetterStore.cs` | `FileManager/Resilience/` |
| `Resilience/DeadLetterEntry.cs` | `FileManager/Resilience/` |
| `Resilience/CsvDeadLetterStore.cs` | `FileManager/Resilience/Implementations/` |
| `Resilience/FileRetryPolicy.cs` | `FileManager/Resilience/` |
| `Resilience/RetryHelper.cs` | `FileManager/Resilience/` |
| `Resilience/IFileArrivalTrigger.cs` | `FileManager/Resilience/` |
| `Resilience/FileSystemWatcherTrigger.cs` | `FileManager/Resilience/Implementations/` |
| `Resilience/IngestionQualityGate.cs` | `FileManager/Resilience/` |
| Unit + integration tests | `tests/FileManager/ResilienceTests.cs` |

---

## 10. Enterprise Standards Traceability

| Standard | Clause | Addressed |
|----------|--------|-----------|
| ISO 22301 | Business Continuity — data recovery | Checkpoint/resume |
| NIST SP 800-53 | SI-11 Error Handling | Dead-letter + error taxonomy |
| SOX | Completeness of data ingestion | Quality gate + rejection rate |
| RPO/RTO SLAs | Max data loss / recovery time | Checkpoint frequency controls both |

# File Persistence Contract

`AtomicFileStore` writes complete UTF-8 snapshots. Callers serialize before
calling `WriteTextAsync`; read/modify/write callers use `UpdateTextAsync` and
serialize their new snapshot inside its callback. A callback must not reenter
the same file store. Returning the original snapshot is a no-op.

## Coordination And Failure

- A persistent `<target>.beep.lock` file coordinates cooperating writers across
  instances and processes. Its exclusive handle is the lease, not its existence.
  Never delete a sidecar to release a lease: that permits two different owners.
- Lease acquisition is cancellable and times out after 30 seconds. Process exit
  releases the handle automatically; a stale sidecar does not block future work.
- Writes use a unique same-directory temporary file, flush it, then replace the
  destination (or move it on first creation). Replacement has bounded retries.
  There is no truncate/copy fallback. Serialization and pre-commit cancellation
  leave the previous destination unchanged; persistent I/O errors propagate.
- Sync entrypoints perform synchronous I/O rather than blocking on async tasks.
  Async lease waits are cancellable/nonblocking, but lease-held read/replace uses
  a non-suspending synchronous critical section. This prevents blocked sync callers
  from starving the pool continuation that would release their lease. It is not
  fully asynchronous disk I/O: a filesystem call can block, and cancellation is
  observed between operations/before commit, not inside a running sync write.
  Keep snapshots bounded; scalable/streaming storage remains separate work.
- Readers use shared snapshot handles. If Windows replacement temporarily makes
  a file missing/inaccessible, they acquire the lease and re-read after the
  writer settles. Writers use a non-reentrant read while already holding it.
- Cancellation is checked before commit. Cancellation after successful replacement
  does not convert the acknowledgement into a cancelled result.
- Normal failures clean up temporary files. A killed process can leave a temporary
  file; it is not authoritative and is never selected automatically as recovery.
  No automatic backup or corrupt-file reset is provided. Operators must preserve
  evidence and explicitly restore a validated backup or clear a store.
- This is a cooperating-writer/local-filesystem contract, not a distributed lease,
  network filesystem guarantee, directory-fsync or power-loss guarantee. All
  readers/writers of a store must use the same normalized path and protocol.

`JsonLoader.Serialize` retains its Newtonsoft serializer settings but now
serializes before disk mutation and throws on failure. Missing files still have
the old default/empty-list read behavior; corrupt files and read errors throw.
Higher-level legacy configuration managers may still log/catch these exceptions;
their error-result contracts remain separate work.

## Migration History

ConfigEditor now implements the optional Models contract
`IMigrationHistoryPersistence`. `SaveMigrationHistoryAcknowledged` and
`AppendMigrationRecordAcknowledged` return `PersistenceWriteResult` with Saved,
Failed, Cancelled or Unsupported status. Saved acknowledges completion of the
coordinated filesystem operation, not power-loss durability. The original void
save/append signatures remain, but now throw on failure instead of hiding it.
Load distinguishes a missing file from empty/corrupt/unreadable existing history.

MigrationHistoryManager uses `IJsonSnapshotCodec` (implemented by JsonLoader) to
freeze complete payloads before replacing files. Custom loaders must explicitly
implement this stable codec; their legacy void Serialize is not assumed to be
safe or acknowledged. Custom IConfigEditor stores must implement the optional
acknowledged history capability before governed DDL can execute. Ambient host
JsonConvert defaults do not configure the built-in snapshot codec.

New histories use StorageFormatVersion=1 and
`Migrations/history-v1-<SHA256 of trimmed, invariant-uppercase datasource name>.json`.
Each append reads, validates and writes under that file's lease. Validate stored
datasource name/type and format; unsupported versions, missing identity/records,
duplicate JSON properties and malformed records are not reset. Repeated migration
IDs are allowed because checkpoints are a sequence of snapshots; this is not a
unique-event or token-concurrency admission mechanism. Whole-history Save is an
explicit replacement, not an optimistic merge of stale caller data.

If the canonical file is absent, load the old sanitized-name file and validate its
embedded identity. The first acknowledged update copies it into the new location
and leaves original bytes intact. Ambiguous/foreign/corrupt legacy files are
rejected. Once promoted, only the canonical file is authoritative; do not run old
writers concurrently, or assume edits to the retained legacy file will be merged.
Legacy filename discovery still uses the current OS's invalid-character rules
and supplied spelling; cross-OS/case-renamed legacy discovery needs explicit
operator migration. Never delete legacy evidence to bypass identity validation.

The filesystem and child-process suites cover coordinated append, corrupt-state
preservation, legacy promotion, cancellation and Windows replacement denial.
Migration tests also reload captured plans/completed checkpoints in a new process
and verify that resume performs no additional DDL. Live-provider crash recovery,
bounded retention, backups and supported Unix qualification remain open gates.

## BeepSync Snapshots

SchemaPersistenceHelper coordinates complete per-schema updates, preserves corrupt
evidence, uses versioned closed typed cursor envelopes and immutable hashed
version/checkpoint artifacts. BeepSyncManager requires acknowledged Running and
Completed saves when checkpointing is enabled. Terminal failure retains acknowledged
counts and requires reconciliation; post-commit diagnostics cannot change Success.
The schema/watermark save remains explicit and is not a transaction with Completed.
Promotion, automatic offset replay, distributed admission, backups/recovery and
supported Unix qualification remain separate gates. See
[sync storage and outcomes](../../Editor/BeepSync/STORAGE-AND-OUTCOMES.md).

## Connection Catalog

`JsonConnectionStorageProvider` coordinates the entire target catalog update,
including synchronous/asynchronous calls and different instances/processes.
Malformed packages, unsupported versions and scope mismatches are not empty
catalogs. Promotion/import reads a source snapshot and updates the target under
its lease; it is not a transaction spanning two files. Project precedence is
above User, which is above Machine. Existing credential protection is unchanged;
the broader protection/fallback audit remains pending.

## Import Stores And Upgrade Behavior

Error/history stores use complete coordinated JSONL snapshots and validate every
record's ContextKey before appending or replay marking. This prioritizes recovery
correctness: appending currently reads/validates the existing history, so append
cost grows with file size. Bounded retention and a scalable log backend remain
Phase 4 work. Batch/record replay identity is not an exactly-once guarantee.

All three import stores accept an explicit folder constructor in addition to
their existing zero-argument constructors. Filenames are SHA-256 hashes of the
case-insensitive context identity with the existing per-store suffix. The context
key must include the caller's datasource/schema identity; the stores do not infer
that identity from an arbitrary label.

Watermarks use a version-1 envelope containing Version, ContextKey and a closed
tagged Value. Supported values are null, strings, booleans, integer primitives,
decimal/float/double, DateTime (ticks and kind), DateTimeOffset (round-trip offset),
Guid, byte arrays, object arrays and string-keyed object dictionaries. Nested
types are preserved; unsupported/cyclic/deep values are rejected. No runtime type
name from JSON is activated. Composite *storage* support does not enable
sequence/composite BeepSync *execution* modes.

Legacy sanitized filenames are explicitly rejected when discovered, without
modification. They lack reliable identity, and legacy watermarks store only a
culture-dependent string with no type. Before migration, preserve the originals,
validate each context against its datasource/schema, and explicitly choose the
cursor type/conversion. Do not simply relabel a legacy string as a timestamp or
delete a legacy file to suppress the error. A migration utility/UI and cross-OS
legacy discovery are not yet implemented.

## Verification

Optional target-level migration admission is defined in
[EXECUTION-OWNERSHIP.md](../../Editor/Migration/EXECUTION-OWNERSHIP.md).
FileMigrationExecutionOwnership holds a permanent exclusive owner handle through
the claim lifetime, separately from short snapshot mutation leases. Dispose/crash
does not clear unfinished durable work; explicit reconciliation cannot clear a
live owner. ConfigEditor exposes the optional Models capability at an explicit
root. MigrationManager integration remains open; this backend is not automatic
execution admission or live-provider recovery.

Connection fallback/catalog acknowledgement, encryption/redaction, key/version
validation and observer boundaries are defined in
[Security/README.md](../../Security/README.md). Atomic replacement alone is not
credential protection. Other configuration and BeepSync writers remain under audit.

`tests/FrameworkReliabilityTests` includes failed serialization/replacement,
cancelled waits, concurrent readers, typed restart, corrupt/foreign identity
rejection, mixed sync/async catalogs, process updates and killed-owner recovery.
`FrameworkPersistenceWorker` is a test-only child-process executable, built and
copied under reliability/migration-test output directories by project references.
One isolated worker caps its thread pool at two threads while contended synchronous
and asynchronous catalog writers update the same file; all updates must complete.

```powershell
dotnet test tests/FrameworkReliabilityTests/FrameworkReliabilityTests.csproj -p:GeneratePackageOnBuild=false
```

Windows net8/net9/net10 verification does not establish Unix or remote filesystem
behavior. See the framework implementation log for the latest executed matrix.

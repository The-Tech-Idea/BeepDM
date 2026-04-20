# Phase 08 — Audit Event Schema & Tamper Evidence

## Objective

Define the **canonical `AuditEvent` v1** that every audit producer must use, and add a **tamper-evident hash chain** so any after-the-fact modification, deletion, or reorder is detectable.

## Dependencies

- Phase 01 forward-declared `AuditEvent` and `AuditQuery` placeholders.

## Scope

- **In**: Schema, categories, hash-chain signer, integrity verifier, sealed-log mode.
- **Out**: Encryption-at-rest (v2). Detached external attestation / blockchain (v2).

## Target files

```
Services/Audit/Models/
  AuditEvent.cs                      // partial: .Core, .ChainFields
  AuditEvent.Core.cs
  AuditEvent.ChainFields.cs
  AuditCategory.cs
  AuditOutcome.cs
  AuditFieldChange.cs
  AuditQuery.cs

Services/Audit/Integrity/
  IHashChainSigner.cs
  HashChainSigner.cs                 // partial: .Core, .Sign, .Verify
  HashChainSigner.Core.cs
  HashChainSigner.Sign.cs
  HashChainSigner.Verify.cs
  IntegrityVerifier.cs
  ChainAnchorStore.cs                // persists last hash per chain id
  SealedLogPolicy.cs
```

## Design notes

### `AuditEvent` v1 (canonical)

```csharp
public partial class AuditEvent
{
    // Identity
    public Guid    EventId       { get; set; } = Guid.NewGuid();
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string  ChainId       { get; set; } = "default";
    public long    Sequence      { get; set; }            // assigned by signer
    public string  PrevHash      { get; set; }            // hex
    public string  Hash          { get; set; }            // hex (HMAC of canonical payload + prevHash)

    // Classification
    public AuditCategory Category { get; set; }           // DataAccess, Auth, Config, Schema, Distributed, Custom
    public string Operation       { get; set; }           // Insert | Update | Delete | Login | Grant | Migrate ...
    public AuditOutcome Outcome   { get; set; } = AuditOutcome.Success;
    public string Reason          { get; set; }           // failure / denial reason

    // Actors / context
    public string UserId          { get; set; }
    public string UserName        { get; set; }
    public string Tenant          { get; set; }
    public string CorrelationId   { get; set; }
    public string TraceId         { get; set; }

    // Subject
    public string Source          { get; set; }           // e.g. "Forms.Block.HR_EMP", "Proxy.Cluster", "Distributed.Shard.S1"
    public string EntityName      { get; set; }
    public string RecordKey       { get; set; }
    public IList<AuditFieldChange> FieldChanges { get; set; } = new List<AuditFieldChange>();

    // Free-form
    public IDictionary<string, object> Properties { get; set; }
}
```

### `AuditCategory`

| Value | Used by |
|---|---|
| `DataAccess`   | CRUD via FormsManager / UnitofWork / IDataSource |
| `Auth`         | Sign-in, sign-out, role grants/revokes |
| `Config`       | Connection / mapping / settings changes via ConfigEditor |
| `Schema`       | DDL via MigrationManager or distributed schema service |
| `Distributed`  | Resharding, plan changes, cross-shard transactions |
| `Custom`       | Operator-defined |

### `AuditOutcome`

`Success` | `Failure` | `Denied` | `Pending` | `Compensated`.

### Hash chain

Per `ChainId` (default = `"default"`; operators may segment by tenant or category):

```
Hash_n = HMAC-SHA256(secret, canonical_json(payload_n) || PrevHash_n)
PrevHash_{n+1} = Hash_n
Sequence_{n+1} = Sequence_n + 1
```

- `secret` is loaded from operator config (env var preferred); rotated per environment.
- `canonical_json` is sorted-key JSON of every field except `PrevHash`/`Hash`/`Sequence` (which are filled by the signer).
- `ChainAnchorStore` persists `(ChainId, LastHash, LastSequence)` to a small SQLite table or a `chain.anchor.json` file so restart is safe.

### `IntegrityVerifier`

Replays a chain (or a date range / sub-chain) and reports the **first divergence**: missing sequence, hash mismatch, prev-hash mismatch, or anchor mismatch. Used by the runbook (Phase 13) and an optional periodic background check.

### Sealed-log mode

Optional — when a file rotates (Phase 04), the closed file is set read-only. On Linux/macOS we additionally `chmod 0440`. This makes accidental in-place edits visible and fits common compliance requirements.

### Compression and chain

Compression (Phase 04) operates on file bytes after the chain field has been written into the JSON envelope. Verification re-reads via `GzipStream`, so the chain remains valid through compression.

### Purge and re-seal

GDPR purge (Phase 10) **rewrites a continuous range** of records by:

1. Removing matching records.
2. Re-computing hashes for each subsequent record in that chain (this re-seals the chain).
3. Writing a `Purge` audit event (in a separate `purge` chain) with the redacted record ids and operator identity.

The purge is itself auditable, and the original chain is internally consistent again. Cross-chain consistency requires the verifier to also walk the `purge` chain.

## Implementation steps

1. Add `AuditEvent` partials, `AuditCategory`, `AuditOutcome`, `AuditFieldChange`, `AuditQuery`.
2. Add `IHashChainSigner` + `HashChainSigner` partials.
3. Add `ChainAnchorStore` (persists last hash; SQLite or JSON depending on host).
4. Add `IntegrityVerifier`.
5. Add `SealedLogPolicy` (post-rotate hook from Phase 04).
6. Hook the signer into `BeepAudit.RecordAsync` so every event is sequenced and hashed before reaching the queue.
7. Tests: chain produced for 10k events; mutate one byte → verifier reports divergence; purge + re-seal still verifies.

## TODO checklist

- [ ] P08-01 `AuditEvent.{Core,ChainFields}.cs` + `AuditCategory.cs` + `AuditOutcome.cs` + `AuditFieldChange.cs` + `AuditQuery.cs`.
- [ ] P08-02 `IHashChainSigner.cs` + `HashChainSigner.{Core,Sign,Verify}.cs`.
- [ ] P08-03 `ChainAnchorStore.cs`.
- [ ] P08-04 `IntegrityVerifier.cs`.
- [ ] P08-05 `SealedLogPolicy.cs` and post-rotate hook.
- [ ] P08-06 Wire signer into `BeepAudit.RecordAsync`.
- [ ] P08-07 Tests for chain, divergence detection, restart, purge re-seal.

## Verification

- Verifier replays a 10k-event chain in < 200 ms on a developer machine.
- Manual byte mutation in any audit file is detected with a precise `(file, sequence, expected, actual)` report.
- After process restart with anchor file present, the next event chains correctly to the prior `LastHash`.
- Compressed (gz) and rotated audit files still verify end to end.

## Risks

- **R1**: Secret leakage breaks tamper evidence. Mitigation: secret loaded from env or DPAPI/KeyChain wrapper (cross-platform via `IKeyMaterialProvider` injected by host); never written to disk.
- **R2**: Reordered enqueueing under high concurrency would corrupt the chain. Mitigation: signer is single-threaded inside `BeepAudit.RecordAsync` (mutex around `Sequence`/`Hash` assignment); enqueue happens *after* signing.
- **R3**: Purge re-seal complexity. Mitigation: purge runs in a maintenance window and is itself transactional at the file level (write to `.tmp`, fsync, rename).

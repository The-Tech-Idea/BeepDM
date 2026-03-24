# Phase 2 (Enhanced) — Schema Governance, Versioning, and Mapping Lifecycle

## Supersedes
`../02-phase2-schema-governance-and-versioning.md`

## Objective
Tie `DataSyncSchema` versioning to `MappingManager` governance lifecycle, stamp schema
metadata via `DefaultsManager` expressions, and enforce approval state transitions.

---

## Scope
- `DataSyncSchema` versioning artifact (hash, version int, immutable baseline).
- Mapping artifact lifecycle linked to schema version.
- Defaults-driven metadata stamping.
- Schema compare/diff and promotion workflow.

---

## File Targets

| File | Change Description |
|---|---|
| `BeepSync/Helpers/SchemaPersistenceHelper.cs` | Add `SaveVersionedSchema(...)`, `LoadSchemaVersion(...)`, `DiffSchemaToPersisted(...)` |
| `BeepSync/Helpers/FieldMappingHelper.cs` | Add `LoadGovernedMapping(...)`, `CheckMappingDrift(...)`, `PromoteMappingState(...)` |
| `BeepSync/Interfaces/ISyncHelpers.cs` | Add `ISchemaPersistenceHelper.SaveVersionedSchema`, `IFieldMappingHelper.LoadGovernedMapping` |
| `BeepSync/Models/SyncSchemaVersion.cs` *(new)* | Version artifact: version int, hash, timestamp, author, approval state |

---

## Integration Points: Mapping Manager

### 1. Versioned Field Map per Schema Version
Each time a `DataSyncSchema` is saved (via `SchemaPersistenceHelper.SaveSchema`), also save
the current field mapping under governance scope:
```csharp
using (MappingManager.BeginGovernanceScope(
    author: DefaultsManager.Resolve(editor, ":USERNAME", null)?.ToString() ?? "system",
    changeReason: $"Schema v{newVersion.Version} save — {schemaId}",
    targetState: MappingApprovalState.Draft))
{
    MappingManager.SaveEntityMap(
        BuildEntityMapFromSyncSchema(schema),
        schema.SourceDataSourceName,
        schema.DestinationDataSourceName);
}
```

### 2. Mapping Version History Audit
Expose `GetSchemaMappingHistory(string schemaId)` that wraps:
```csharp
MappingManager.GetMappingVersionHistory(sourceDsName, destDsName, entityPair)
```
Returns version list with author, timestamp, change reason, and approval state for UI/CLI display.

### 3. Mapping Diff on Schema Compare
When a schema version diff is requested, also compute the mapping diff:
```csharp
var mappingDiff = MappingManager.BuildMappingVersionDiffText(
    sourceDsName, destDsName, entityPair,
    fromVersion, toVersion);
preflight.MappingDiff = mappingDiff;
```

### 4. Approval State Promotion
`PromoteSchemaVersion(schemaId, version, targetState)` also calls:
```csharp
MappingManager.UpdateMappingApprovalState(sourceDsName, destDsName, entityPair, targetState);
```
Ensures mapping and schema states are co-promoted (both Draft → Review → Approved together).

---

## Integration Points: Defaults Manager

### 1. Stamp Schema Version Metadata
On every schema save:
```csharp
// Expression-resolved fields
var savedBy  = DefaultsManager.Resolve(editor, ":USERNAME", null)?.ToString() ?? "system";
var savedAt  = (DateTime)DefaultsManager.Resolve(editor, ":NOW", null);
var versionId = DefaultsManager.Resolve(editor, ":NEWGUID", null)?.ToString();

var artifact = new SyncSchemaVersion
{
    SchemaId     = schema.SchemaID,
    Version      = schema.SchemaVersion + 1,
    SavedAt      = savedAt,
    SavedBy      = savedBy,
    VersionGuid  = versionId
};
```

### 2. Default Profile for Schema Versioning Entity
```csharp
DefaultsManager.SetColumnDefault(editor, "SyncSchemas", "SyncSchemaVersion",
    "ApprovalState", "Draft", isRule: false);
DefaultsManager.SetColumnDefault(editor, "SyncSchemas", "SyncSchemaVersion",
    "IsActive", "true", isRule: false);
```

---

## Integration Points: Rule Engine

### (Minimal in this phase)
No direct rule evaluation in schema governance. Rules are referenced only in
`SyncRulePolicy.CatalogVersion` — the catalog version string that should be co-versioned
alongside the schema version to prevent rule drift.

Rule key to add:
| Rule Key | Stage | Behaviour |
|---|---|---|
| `sync.schema.promotion-gate` | Promotion to Approved | Verifies mapping state = Approved + no open conflicts |

---

## `SyncSchemaVersion` Model

```csharp
public class SyncSchemaVersion
{
    public string SchemaId        { get; set; }
    public int    Version         { get; set; }
    public string VersionGuid     { get; set; }
    public string SchemaHash      { get; set; }       // SHA256 of serialized schema
    public DateTime SavedAt       { get; set; }
    public string SavedBy         { get; set; }
    public string ApprovalState   { get; set; }        // "Draft" | "Review" | "Approved"
    public string MappingVersion  { get; set; }        // co-versioned mapping artifact ref
    public string RuleCatalogVersion { get; set; }     // rule catalog version at save time
    public string ChangeNotes     { get; set; }
}
```

---

## Acceptance Criteria
- Every `SaveSchema(...)` call produces a `SyncSchemaVersion` artifact with resolved author/timestamp.
- Mapping and schema states are co-promoted atomically (both or neither).
- `GetSchemaMappingHistory(...)` returns version list usable for PR/approval review.
- Schema promotion is blocked when mapping approval state is below `SyncMappingPolicy.RequiredApprovalState`.
- Schema hash is computed deterministically and stored with the version artifact.

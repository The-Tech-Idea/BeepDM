# Mapping

Entity and object mapping utilities.

For independent run definitions, Models `EntityMetadataSnapshot.Capture` copies
supported entity metadata and nested options with key aliases preserved and no
source event observers. Legacy EntityStructure.Clone remains shallow. Read
[snapshot contract](../../../DataManagementModelsStandard/DataBase/METADATA-SNAPSHOTS.md)
for unsupported graphs, limits and caller coordination. Generated types now use
[source-sensitive identity](../../ConfigUtil/GENERATED-TYPES.md); bare-name seeds
cannot override metadata. Existing-target mapped sync now binds captured actual
provider fields in both directions; read
[sync admission contract](../BeepSync/STORAGE-AND-OUTCOMES.md). Mapped creation,
record DQ and collectible assembly lifetime remain open integration gates.

## Key Components
- `MappingManager` (static facade)
- `MappingManager.Conventions.cs` (convention mapping, validation, diff)
- `MappingManager.AutoMatching.cs` (weighted matching + confidence/explainability)
- `MappingManager.ConversionPipeline.cs` (conversion policy and transform pipeline)
- `MappingManager.ObjectGraph.cs` (nested path + object graph + collection mapping)
- `MappingManager.Rules.cs` (conditional rules, precedence, and rule validation)
- `MappingManager.ValidationScoring.cs` (quality score, risk bands, drift detection)
- `MappingManager.PerformanceCaching.cs` (compiled mapping plans, accessor caches, cache invalidation)
- `MappingManager.Governance.cs` (versioning, approval lifecycle hooks, audit trail)
- Auto object mapper stack:
  - `Configuration/*`
  - `Core/*`
  - `Models/*`
  - `Interfaces/*`
  - `Helpers/*`
  - `Extensions/*`
  - `Utilities/*`

## Folder Architecture
- `Configuration`: type-map registration and mapper option models.
- `Core`: AutoObjMapper runtime engine, expression compilation, validation/performance/factory layers.
- `Models`: shared DTO-like mapping result and diff models for mapper workflows.
- `Interfaces`: contracts for mapper configuration/execution abstractions.
- `Helpers`: defaults, validation, reflection discovery, conversion, and perf helper utilities.
- `Extensions`: fluent APIs and convenience wrappers around core mapper APIs.
- `Utilities`: static helper methods reused across mapper runtime code.

## Responsibilities
- Build entity maps between source and destination entities.
- Auto-map fields by name with type metadata (`FromFieldType`, `ToFieldType`).
- Provide scored suggestions (`AutoMapByConventionWithScoring`) for auto-accept/review/reject decisions.
- Support per-entity conversion policies and per-field transform chains.
- Support nested-path/object-graph mapping through `MapObjectGraph(...)` with:
  - max depth
  - cycle detection
  - collection modes (`append`, `replace`, `merge-by-key`)
- Support rule-based field behavior with deterministic precedence:
  - explicit user rule
  - confidence auto-map
  - fallback default
- Provide quality gates before execution:
  - score (0-100)
  - quality band
  - drift detection report
  - production threshold enforcement
- Use compiled mapping plans for repeated execution to reduce reflection overhead.
- Invalidate mapping cache deterministically on mapping save/update.
- Record governance metadata per save (version, author, timestamp, reason, approval state).
- Maintain mapping audit trail and version history sidecar for traceability.
- Persist mappings through `MappingManager.SaveEntityMap(...)`.
- Transform source objects into destination entity instances.
- Apply defaults post-mapping via mapping helper integration.

## Mapping Persistence
- Stored as JSON under `Config.MappingPath/{datasource}/{entity}_Mapping.json`.
- Loaded through `ConfigEditor.LoadMappingValues(entity, datasource)`.

## Typical Usage
1. Create or load map (`CreateEntityMap`, `LoadMappingValues`).
2. Adjust field map details as needed.
3. Save map.
4. Transform records (`MapObjectToAnother`) during import/migration.

## Governance Usage
- Wrap save operations in `BeginGovernanceScope(...)` to stamp author/reason/state.
- Query `GetMappingVersionHistory(...)` to inspect saved versions.
- Use `UpdateMappingApprovalState(...)` to promote mapping states across environments.
- Use `BuildMappingVersionDiffText(...)` for approval and release notes.

```csharp
using (MappingManager.BeginGovernanceScope(
    author: "etl-ops",
    changeReason: "Align legacy email field mapping",
    targetState: MappingApprovalState.Review))
{
    var (_, mapping) = MappingManager.CreateEntityMap(
        editor, "LegacyCustomers", "LegacyDb", "Customers", "MainDb");

    // edit mapping.MappedEntities[0].FieldMapping as needed
    MappingManager.SaveEntityMap(editor, "Customers", "MainDb", mapping);
}
```

## Examples
- See `Editor/Mapping/Examples/README.md` for scenario-based guides:
  - scored auto matching
  - conversion/rule/object graph pipelines
  - quality and drift gates
  - performance caching behavior
  - governance/version/audit lifecycle

## Safety Notes
- Mapping uses compiled plans first, with reflection fallback for unsupported paths.
- Missing properties are logged and processing continues.
- Validate map definitions before high-volume migrations.
- Keep production execution pinned to approved mapping versions where governance is enabled.

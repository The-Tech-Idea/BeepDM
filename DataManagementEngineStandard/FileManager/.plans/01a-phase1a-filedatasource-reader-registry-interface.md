# Phase 1A - FileDataSource Reader Registry Interface

## Objective

Introduce a dedicated `FileDataSource`-level interface so callers can:

1. inspect available readers discovered by registry/factory,
2. switch active reader at runtime, and
3. resolve reader metadata (`FileReaderDescriptor`) in a uniform way.

This phase closes the gap between:
- `FileReaderFactory` (static runtime reader map), and
- `FileReaderRegistry` (descriptor/discovery model),

so `FileDataSource` can expose both runtime behavior and discoverability safely.

## Why this phase is needed

Current `FileDataSource.Openconnection()` resolves `_reader` directly from `FileReaderFactory.GetReader(DatasourceType)` and does not expose:
- available readers list,
- selected descriptor metadata,
- an explicit switch API for reader override.

This makes UI/command tooling harder (no official API to query/switch readers).

## Proposed Contract

Create a new interface (name proposal):

- `IFileDataSourceReaderHost`

**Location requirement (updated):**
- Create it in `BeepDM/DataManagementModelsStandard/FileManager` so it is shared/consumable across engines and plugins without depending on EngineStandard internals.

Suggested members:

```csharp
IReadOnlyList<FileReaderDescriptor> GetAvailableReaderDescriptors(bool discover = false);
IReadOnlyList<DataSourceType> GetAvailableReaderTypes();
FileReaderDescriptor? GetCurrentReaderDescriptor();
IFileFormatReader? GetCurrentReader();
bool TrySwitchReader(DataSourceType targetType, bool reconfigure = true, out string? reason);
void ResetReaderSelection();
```

Optional extension:

```csharp
bool TrySwitchReaderByExtension(string extension, bool reconfigure = true, out string? reason);
```

## Scope

- `DataManagementModelsStandard/FileManager/IFileDataSourceReaderHost.cs`
- `DataManagementEngineStandard/FileManager/FileDataSource.cs`
- `DataManagementEngineStandard/FileManager/FileDataSource.Connection.cs`
- `DataManagementEngineStandard/FileManager/Registry/FileReaderRegistry.cs`
- `DataManagementEngineStandard/FileManager/FileReaderFactory.cs`

## Execution TODOs

### Sprint A - Contract Definition

- [x] Add `IFileDataSourceReaderHost` interface with methods listed above in:
  - `DataManagementModelsStandard/FileManager/IFileDataSourceReaderHost.cs`
- [x] Add XML docs including behavior for discovery vs cached mode.
- [x] Define whether `TrySwitchReader` updates `DatasourceType` (recommended: yes, only on success).
- [x] Ensure `DataManagementEngineStandard` references the updated ModelsStandard contract (if not already available through existing references).

### Sprint B - FileDataSource Implementation

- [x] Implement `IFileDataSourceReaderHost` on `FileDataSource` partial class.
- [x] Add internal fields:
  - current descriptor,
  - optional registry instance cache,
  - last switch reason.
- [x] Ensure `Openconnection()` uses current selected reader when overridden; otherwise defaults by `DatasourceType`.
- [x] Keep backward compatibility: existing callers that rely on `DatasourceType` continue to work unchanged.

### Sprint C - Registry + Factory Integration Rules

- [x] Add helper path to get descriptor list from `FileReaderRegistry` (discover optional).
- [x] Add fallback behavior when registry is unavailable:
  - report factory-supported types only.
- [x] Ensure switch failure returns non-throwing reason message where possible.

### Sprint D - Diagnostics and Safety

- [x] Add logs for:
  - discovered readers count,
  - switch success/failure,
  - fallback to default reader.
- [x] Reject switching while connection is actively ingesting (or explicitly define safe behavior).
- [x] Ensure `Configure(IConnectionProperties)` is called after successful switch.

### Sprint E - Consumer Enablement (Winform/Shell/Commands)

- [ ] Add one reference usage sample in docs (`README.md` or dedicated section):
  - list readers,
  - switch to target type,
  - reopen connection.
- [ ] Wire one command/UI path that calls the new host contract (optional in this phase, required by next phase if deferred).

## Acceptance Criteria

- [x] `FileDataSource` exposes a public reader-host contract for list/switch/inspect.
- [x] Reader switching is deterministic and does not break existing default-open flow.
- [x] Descriptor-level metadata can be retrieved when registry is available.
- [ ] Unit/integration tests cover:
  - switch to supported type,
  - switch to unsupported type,
  - switch when registry discovery has not run yet,
  - switch then `Openconnection()` uses new reader.

## Verification Checklist

- [x] Verify `GetAvailableReaderTypes()` includes factory registrations.
- [x] Verify `GetAvailableReaderDescriptors(discover:true)` includes `[FileReader(...)]` readers.
- [ ] Verify switching `CSV -> TSV -> Json` updates active reader and parser behavior.
- [ ] Verify null/missing descriptors return clear reason without crash.

## Dependencies

- Depends on: `01-phase1-contracts-and-reader-abstractions.md`
- Should precede/align with:
  - `08-phase8-format-expansion-and-plugin-model.md`
  - `10-phase10-rollout-observability-and-kpis.md`

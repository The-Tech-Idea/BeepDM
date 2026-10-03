---
name: configeditor
description: Guidance for ConfigEditor usage, delegated managers, and persisted configuration files in BeepDM. Use when loading, updating, or saving data connections, query metadata, mappings, drivers, workflows, reports, projects, or migration history.
---

# ConfigEditor Guide

Use this skill when working with BeepDM configuration persistence and the manager façade exposed through `IConfigEditor`.

## Use this skill when
- Loading or saving `DataConnections`
- Working with query metadata, mappings, and entity structures
- Managing component metadata such as drivers, workflows, reports, and projects
- Understanding how config paths and specialized managers are composed

## Do not use this skill when
- The main problem is building a connection definition itself. Use [`connectionproperties`](../connectionproperties/SKILL.md).
- The main problem is validating or securing connection strings. Use [`connection`](../connection/SKILL.md).
- The main problem is opening and using the datasource. Use [`beepdm`](../beepdm/SKILL.md).

## Core Managers
- `ConfigPathManager`: config root and folder structure
- `DataConnectionManager`: `DataConnections` CRUD and persistence
- `QueryManager`: `QueryList` operations and default query initialization
- `EntityMappingManager`: entity metadata, mappings, and datasource entities
- `ComponentConfigManager`: drivers, workflows, reports, projects, add-ins, and component metadata
- `MigrationHistoryManager`: per-datasource migration history

## Responsibilities
- Treat `ConfigEditor` as the façade; prefer its public methods over directly manipulating underlying managers.
- Persist configuration to the configured app/container path.
- Keep in-memory collections and on-disk configuration synchronized.
- Provide the metadata needed by datasource creation, migrations, ETL, and UI tooling.

## Acknowledged Migration History
- Use optional Models `IMigrationHistoryPersistence` on ConfigEditor for `SaveMigrationHistoryAcknowledged`/`AppendMigrationRecordAcknowledged`. Inspect `PersistenceWriteResult.Status` (Saved, Failed, Cancelled, Unsupported); legacy void history save/append now throw on failure.
- History reads return empty only for a missing file, not corrupt/unreadable/empty existing state. Append coordinates the full read/validate/write across cooperating instances/processes; whole-history Save is explicit replacement, not stale-snapshot merging.
- Built-in JsonLoader implements `IJsonSnapshotCodec` for stable complete snapshots. Custom loaders/stores need the explicit capabilities; never assume a void serializer persisted successfully.
- New files are `Migrations/history-v1-<SHA256 of trimmed invariant-uppercase datasource name>.json` with StorageFormatVersion=1. Validate stored name/type before updating.
- Valid legacy sanitized-name history is promoted on first update without altering original bytes; foreign/corrupt identity is rejected. After promotion, only the canonical file is authoritative. Do not mix old writers or delete legacy evidence to bypass validation.
- See `DataManagementEngineStandard/Services/Persistence/README.md` for legacy case/OS discovery, local-filesystem, cancellation and power-loss limits. Broader configuration/BeepSync persistence remains unfinished.

## Protected Connection Persistence
- ConfigEditor implements optional Models `IConnectionConfigurationPersistence`. Inspect `SaveDataConnectionsAcknowledged(token).Status`; legacy `SaveDataconnectionsValues()` throws on failed saves. Custom catalog Save(false) is failure, never permission for raw-file fallback.
- Fallback snapshots and built-in catalogs use captured per-runtime `IConnectionSecretProtector`. Existing corrupt/undecryptable state blocks replacement; load changes live state only after complete validation. Whole-snapshot Save is explicit replacement, not stale-edit merging.
- Default `ConnectionCredentialProtection` uses Windows DPAPI CurrentUser. For portable hosts, inject `AesGcmConnectionCredentialCipher` with a host-owned `IConnectionCredentialKeyProvider` through `BeepServiceOptions.ConnectionSecretProtector` before resolution/configuration. Keep old keys during rotation; never store keys with connections or silently use plaintext.
- Coverage is a defined whitelist: named secrets plus ConnectionString, ParameterList, HTTP/header/parameter containers and authentication URLs. Arbitrary labels/metadata are not a vault. Returned snapshots deep-clone covered containers; runtime drivers need Unprotect, not ciphertext.
- Built-in catalog writes/exports use package version 2.0. Legacy 1.0 plaintext/named-field DPAPI can be read/upgraded; 1.0 records carrying the new opaque payload are rejected. Upgrade every reader/writer; fallback arrays are not version-gated against old loaders.
- Encrypted export decrypts/reprotects with the selected key policy; it is not automatically portable across users/hosts. Redacted export clears whole covered containers and the payload without requiring keys, including operational URLs/strings that hosts must reconfigure.
- BeepConnectionRepository invokes captured subscribers individually outside scope locks. Observer errors cannot reclassify a saved write; concrete NotificationFailed reports operation/scope/exception type only. Notifications are synchronous refresh signals, not ordered durable snapshots.
- Read `DataManagementEngineStandard/Security/README.md` for exact coverage, formats, key/identity binding, recovery and scope/platform limits. Windows process tests do not establish Unix, arbitrary adapter or all-route security guarantees.

## Typical Workflow
1. Access `editor.ConfigEditor`; avoid creating ad-hoc config stores once the editor exists.
2. Load the relevant collection such as `LoadDataConnectionsValues()`.
3. Update through façade methods like `AddDataConnection`, `SaveDataconnectionsValues`, `SaveQueryFile`, or `SaveMappingValues`.
4. Refresh in-memory collections when code depends on newly persisted state.
5. Let downstream systems consume config through `IDMEEditor`, not duplicate file parsing.

## Validation and Safety
- After adding or changing a connection, call `SaveDataconnectionsValues()`.
- Use `LoadDataConnectionsValues()` when you need a refreshed in-memory snapshot.
- Initialize or refresh `QueryList` before operations that depend on generated SQL metadata.
- Keep config paths stable unless you are intentionally migrating application storage.

## Pitfalls
- Bypassing façade methods can desync in-memory collections from persisted files.
- Renaming config files or changing folder layout breaks existing tools and app assumptions.
- Replacing `Config` or re-initializing paths without updating dependent managers causes stale references.
- Creating a second `ConfigEditor` for the same app context fragments state and makes debugging harder.

## File Locations
- `DataManagementEngineStandard/ConfigUtil/ConfigEditor.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/DataConnectionManager.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/QueryManager.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/EntityMappingManager.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/ComponentConfigManager.cs`
- `DataManagementEngineStandard/ConfigUtil/Managers/MigrationHistoryManager.cs`

## Example
```csharp
var config = editor.ConfigEditor;

// Load existing connections
var connections = config.LoadDataConnectionsValues();

// Add a new connection
var props = new ConnectionProperties
{
    ConnectionName = "MyDb",
    DatabaseType = DataSourceType.SqlLite,
    Category = DatasourceCategory.RDBMS,
    ConnectionString = "Data Source=./Beep/dbfiles/app.db"
};
config.AddDataConnection(props);
config.SaveDataconnectionsValues();
```

## Task-Specific Examples

### Initialize Query List Defaults
```csharp
var queries = config.InitQueryDefaultValues();
config.QueryList = queries;
config.SaveQueryFile();
```

### Persist Mapping For Entity
```csharp
config.SaveMappingValues("Customers", "MyDb", mapping);
```

## Related Skills
- [`connectionproperties`](../connectionproperties/SKILL.md)
- [`connection`](../connection/SKILL.md)
- [`beepdm`](../beepdm/SKILL.md)
- [`migration`](../migration/SKILL.md)

## Integration with the data-management layer

`ConfigEditor` is the **persisted-state façade**. Every other data-management layer reads or writes through it:

| Direction | Layer | What flows |
|---|---|---|
| ← **setup** | Setup Framework (Phase 3) | `ConnectionConfigStep` writes first-run connections through this façade. |
| ↔ **migration** | `MigrationManager` | Reads `IsMigrationApplied(ds, name)` and calls `RecordMigration(...)` after success. |
| ↔ **etl** | Pipeline engine | Reads `EntityDataMap` from `EntityMappingManager`; does not invent its own mapping store. |
| → **unitofwork** | `UnitofWork<T>` | UoW operates against an already-configured datasource; it does not write to config. |
| → **forms** | `FormsManager` | Forms read entity structure from config cache, falling back to runtime discovery. |

The Mavis cross-project equivalent of this skill lives at `.harness/skills/beepdm-configuration/SKILL.md`.

## Detailed Reference
Use [`reference.md`](./reference.md) for quick save/load snippets and manager-specific examples.

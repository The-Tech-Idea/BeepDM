---
name: beepdm-configuration
description: Use when loading, updating, or saving persisted BeepDM configuration — connections, queries, mappings, drivers, workflows, reports, projects, or migration history — through the ConfigEditor facade. Hands off to Setup (first run), Migration (history reads), and ETL (mappings) skills.
---

# beepdm-configuration

`ConfigEditor` is the **persisted-configuration facade** for BeepDM. It owns the JSON config files under the app container path and exposes focused sub-managers so the rest of the engine reads/writes through a single, stable API.

## When to use this skill

- Adding / removing / updating a `ConnectionProperties` entry.
- Loading or saving `QueryList` (SQL templates for metadata discovery).
- Saving or reading `EntityDataMap` mappings between entity and datasource.
- Registering a new `ConnectionDriversConfig` (driver class metadata).
- Recording / querying migration history per datasource.
- Inspecting the config path on the current platform.

## Do NOT use this skill for

- Hosting lifetimes, runtime graph ownership, or datasource creation/disposal -> use
  [beepdm-runtime](../beepdm-runtime/SKILL.md). Deferred AddBeepRuntime snapshots
  options; a legacy builder's eager return is caller-owned and separate from DI.
- Building a connection definition for the first time on a fresh machine → use **beepdm-setup**.
- Designing / applying schema migrations → use **beepdm-migration**.
- Bulk data movement → use **beepdm-etl**.

## File Locations

`DataManagementEngineStandard/ConfigUtil/`:

- `ConfigEditor.cs` — façade
- `Managers/ConfigPathManager.cs` — config root and folder structure
- `Managers/DataConnectionManager.cs` — connection CRUD
- `Managers/QueryManager.cs` — `QueryList` operations + default init
- `Managers/EntityMappingManager.cs` — entity metadata + mappings
- `Managers/ComponentConfigManager.cs` — drivers, workflows, reports, projects
- `Managers/MigrationHistoryManager.cs` — per-datasource migration history

There is also a separate **app-level** config layer at `DataManagementEngineStandard/Configuration/` (`AppSettings.cs`, `ISettingsProvider.cs`) — use that for environment-wide settings, not for per-datasource metadata. The two layers are deliberately separate.

## Architecture

```
ConfigEditor (Facade)
├── ConfigPathManager              # Config root and folder structure
├── DataConnectionManager          # Connection CRUD + persistence
├── QueryManager                   # QueryList operations
├── EntityMappingManager           # Entity metadata + mappings
├── ComponentConfigManager         # Drivers, workflows, reports, projects
└── MigrationHistoryManager        # Per-datasource migration history
```

## Sub-managers — when to use which

| Sub-manager | Use it for | Example API |
|---|---|---|
| `ConfigPathManager` | Resolving the container's config root on this OS. | `editor.ConfigEditor.ConfigPath` |
| `DataConnectionManager` | CRUD on `ConnectionProperties`. | `LoadDataConnectionsValues()`, `AddDataConnection(props)`, `SaveDataconnectionsValues()` |
| `QueryManager` | `QueryList` for metadata-discovery SQL. | `InitQueryDefaultValues()`, `SaveQueryFile()` |
| `EntityMappingManager` | Saving/loading `EntityDataMap` per (entity, datasource). | `SaveMappingValues(entity, ds, map)`, `LoadMappingValues(entity, ds)` |
| `ComponentConfigManager` | Driver, workflow, report, project metadata. | `DataDriversClasses`, `SaveConfigValues()` |
| `MigrationHistoryManager` | Recording which migrations ran against which datasource. | `LoadMigrationHistory(ds)`, `AppendMigrationRecord(ds, type, record)`, `AppendMigrationRecordAcknowledged(ds, type, record)` |

## Configuration Files

| File | Owner | Purpose |
|---|---|---|
| `DataConnections.json` | `DataConnectionManager` | Connection definitions |
| `ConnectionConfig.json` | `ComponentConfigManager` | Driver class metadata |
| `DataTypeMapping.json` | `DataTypesHelper` | Type translation between sources |
| `QueryList.json` | `QueryManager` | SQL templates for metadata discovery |
| `Migrations/history-v1-{hash}.json` | `MigrationHistoryManager` | Per-datasource applied-migration log |

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

1. Access `editor.ConfigEditor`; **avoid creating ad-hoc config stores** once the editor exists.
2. Load the relevant collection (`LoadDataConnectionsValues()`, etc.).
3. Update through the façade methods (`AddDataConnection`, `SaveDataconnectionsValues`, `SaveQueryFile`, `SaveMappingValues`).
4. Refresh in-memory collections when downstream code depends on the new state.
5. Let other systems consume config through `IDMEEditor`, not duplicate file parsing.

## How this skill works with the rest of the data-management layer

| Handoff | Direction | What flows |
|---|---|---|
| **beepdm-setup** | ← Setup | `ConnectionConfigStep` writes connections through this façade. First-run connections land in `DataConnections.json`. |
| **beepdm-migration** | ↔ Migration | Governed migration reads captured history and requires acknowledged checkpoints around provider attempts. `MigrationHistoryManager` is the persisted source of truth. |
| **beepdm-etl** | ↔ ETL | ETL reads `EntityDataMap` from `EntityMappingManager` to know how source fields map to target fields. ETL does not maintain its own mapping store. |
| **beepdm-unitofwork** | ← UoW | UoW does not write to `ConfigEditor`; it operates against an already-configured datasource. |
| **beepdm-forms** | ← Forms | Forms read entity structure from config when present, but fall back to runtime discovery via `IDataSource.GetEntityStructure`. |

## Pitfalls

- `JsonLoader.Serialize` serializes first, uses `Services/Persistence/AtomicFileStore`,
  and propagates serialization/I/O failures. Corrupt JSON reads now throw rather
  than returning a missing-file default; legacy managers may still catch/log.
- Catalog changes coordinate the full read/modify/write across sync/async calls,
  instances and cooperating processes. Do not replace this with separate load/save
  calls or delete persistent `.beep.lock` files to unlock a store.
- Malformed/foreign catalog data is not an empty configuration. Preserve it and
  recover explicitly. Built-in fallback/catalog protection is explicit, but
  other serializers/adapters still require audit; atomic writes alone do not secure credentials.
- See `DataManagementEngineStandard/Services/Persistence/README.md` for platform,
  cancellation, recovery and ownership limits.

- **Bypass the façade** → in-memory collections desync from on-disk files. Always use the public methods.
- **Rename config files / move folders** → existing tools and app assumptions break. Treat the layout as a public contract.
- **Replace `Config` mid-run** → dependent managers hold stale references. Initialize once, reuse.
- **Two `ConfigEditor` instances for the same app** → state fragments. Use DI to share a single instance.
- **Forget `Save*` after a mutation** → the change is in memory only. Persist before expecting downstream readers to see it.

## Cross-references

- See **beepdm-setup** for the wizard that writes first-run config.
- See **beepdm-migration** for the history manager it shares with.
- See **beepdm-etl** for the mapping manager it shares with.
- See the directly installed `configeditor` skill for manager-level guidance;
  this checkout uses `.harness/skills`, not the historical `.cursor` paths.

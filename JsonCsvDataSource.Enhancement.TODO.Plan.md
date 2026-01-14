# JSON + CSV DataSources — Enhancement TODO & Plan

Date: 2026-01-14  
Scope: `DataManagementEngineStandard/Json/*` and `DataManagementEngineStandard/FileManager/*`

## Goals
- **Correctness**: reliable schema inference, safe CRUD, correct parsing (CSV quoting), consistent filtering.
- **Scalability**: true streaming + async paths; avoid full in-memory loads for paging.
- **Consistency**: align behaviors across `JsonDataSource`, `JsonDataSourceAdvanced`, and `CSVDataSource`.
- **Observability**: improved errors (`IErrorsInfo`), progress (`IProgress<PassedArgs>`), and logging.

## Non-goals (for this enhancement cycle)
- Changing public interfaces (e.g., `IDataSource`, `IDMEEditor`) or DI lifetimes.
- Introducing new external dependencies unless clearly justified.
- Building UI/UX changes in desktop/web apps.

## Current State (high-level)

### JSON
Primary files:
- `DataManagementEngineStandard/Json/JsonDataSource.cs`
- `DataManagementEngineStandard/Json/JsonDataSourceAdvanced.cs`
- `DataManagementEngineStandard/Json/Helpers/*`

Notable methods exposed (examples):
- `GetEntitesList()`, `GetEntity(...)`, `GetEntityAsync(...)`, `RunQuery(...)`
- `CreateEntityAs(...)`, `CreateEntities(...)`, `CheckEntityExist(...)`
- `SaveJson(...)`, `ReadJson(...)`, `SynchronizeEntityStructure()`, `ValidateSchema()`, `HandleSchemaChanges()`

Observation:
- The “advanced” datasource is helper-driven and has clear seams for caching/filtering/schema/crud.
- Some advanced members are stubbed / behave as “not supported” (e.g., `ExecuteSql`, some bulk updates).
- There are helpers present that are **not fully wired into the public read/paging path yet** (async streaming, schema sync, graph hydration).

### CSV
Primary files:
- `DataManagementEngineStandard/FileManager/CSVDataSource.cs`
- `DataManagementEngineStandard/FileManager/TextFieldParser.cs`
- `DataManagementEngineStandard/FileManager/ICSVDataReader.cs`
- `DataManagementEngineStandard/FileManager/CSVTypeMapper.cs`
- `DataManagementEngineStandard/FileManager/CSVAnalyser.cs`

Notable methods exposed (examples):
- Connection: `Openconnection()`, `Closeconnection()`, `GetFileState()`
- Data: `GetEntity(...)`, `GetEntityAsync(...)`, `GetEntity(...page...)`, `RunQuery(...)`
- CRUD: `InsertEntity`, `UpdateEntity`, `DeleteEntity`, `BulkInsert`
- Transactions: `BeginTransaction`, `Commit`, `Rollback`
- Export/Readers: `ExportDataToCSV(...)`, `GetDataReader(...)`

Observation:
- CSV has a streaming reader abstraction (`ICSVDataReader`), but several hot paths still read all rows then page.
- CSV parsing correctness depends heavily on properly handling quotes + embedded delimiters.

---

## Enhancement Backlog (Prioritized)

### P0 — Must Fix (Correctness / Data Safety)

#### JSON
1. **Persist CRUD changes reliably**
   - Ensure `InsertEntity/UpdateEntity/DeleteEntity/Commit` write back to the JSON file (atomic save).
   - Make persistence strategy explicit (temp file + replace; optional backup).
2. **Unify filtering semantics**
   - Route filtering through `JsonFilterHelper` consistently across `GetEntity`, `RunQuery`, and paging.
3. **Schema drift safety**
   - Ensure schema changes are either (A) rejected with a clear error, or (B) synced via schema helper logic.
   - Document the chosen policy.

#### CSV
1. **Correct CSV parsing for quoted fields**
   - Ensure `GetEntity` uses `TextFieldParser` (or `ICSVDataReader`) so quoted delimiters/newlines don’t corrupt rows.
2. **Fix export/data mismatch**
   - Ensure `ExportDataToCSV` exports the correct data type and doesn’t assume `DataTable` when methods return `PagedResult`/`IEnumerable<object>`.

### P1 — Performance (Streaming / Memory)

#### JSON
1. **True streaming + async**
   - Wire `GetEntityAsync`/paged reads to `JsonAsyncDataHelper` to avoid materializing all records.
2. **PagedResult without full list**
   - Implement page extraction by streaming + counting, not `.Skip/.Take` on a full list.

#### CSV
1. **Streaming paging**
   - Implement page extraction via `ICSVDataReader` and stop after `pageSize` rows collected.
2. **Projection**
   - Support selecting columns early (via `GetDataReader(entityName, columns)`), improving speed.

### P2 — Feature Completeness

#### JSON
1. **Graph hydration API**
   - Expose an optional “hydrate relations” mode using `JsonGraphHelper`.
2. **Schema sync tooling**
   - Add an explicit “sync schema from data” command or method (using existing sync helper).

#### CSV
1. **Better type inference**
   - Integrate `CSVAnalyser` results into `Openconnection()` / schema inference.
2. **Culture-aware parsing**
   - Add configurable culture/timezone for decimal/date parsing.

### P3 — Observability / DX
- Standardize returned `IErrorsInfo` messages with actionable context (entity, file path, line/record).
- Consistent `IProgress<PassedArgs>` reporting on long operations (bulk insert/export/large reads).
- Add structured log messages (via `IDMLogger`) for open/close/read/write and schema changes.

---

## Plan (Phases)

### Phase 1 — Baseline Safety + Tests (P0)
Deliverables:
- JSON CRUD persistence is correct and durable.
- CSV parsing handles quotes correctly.
- Export fixed.

Work items:
- Add/confirm JSON save-on-commit flow (atomic write).
- Use streaming parser for CSV reads in `GetEntity`.
- Fix CSV export to use data reader or existing `GetEntity` output.

Validation:
- Add minimal targeted tests (if tests exist nearby) OR lightweight integration checks in `tests/`.

### Phase 2 — Streaming + Paging (P1)
Deliverables:
- JSON `GetEntityAsync` streams.
- JSON paging does not require full list.
- CSV paging uses `ICSVDataReader`.

### Phase 3 — Feature Completion (P2)
Deliverables:
- Optional JSON graph hydration.
- CSV schema/type inference improvements.

### Phase 4 — Observability polish (P3)
Deliverables:
- Better progress and logs for large operations.

---

## Concrete File Touchpoints

### JSON
- `DataManagementEngineStandard/Json/JsonDataSource.cs`
  - Ensure `SaveJson/ReadJson` usage aligns with CRUD/commit.
  - Verify schema sync functions (`SynchronizeEntityStructure`, `HandleSchemaChanges`).
- `DataManagementEngineStandard/Json/JsonDataSourceAdvanced.cs`
  - Wire async paths + paging to helpers.
  - Ensure `Commit` actually persists.
- `DataManagementEngineStandard/Json/Helpers/JsonCrudHelper.cs`
- `DataManagementEngineStandard/Json/Helpers/JsonAsyncDataHelper.cs`
- `DataManagementEngineStandard/Json/Helpers/JsonFilterHelper.cs`
- `DataManagementEngineStandard/Json/Helpers/JsonSchemaPersistenceHelper.cs`
- `DataManagementEngineStandard/Json/Helpers/JsonSchemaSyncHelper.cs`
- `DataManagementEngineStandard/Json/Helpers/JsonGraphHelper.cs`

### CSV
- `DataManagementEngineStandard/FileManager/CSVDataSource.cs`
  - Replace manual string splitting paths with `TextFieldParser` or `ICSVDataReader`.
  - Implement streaming paging and fix export.
- `DataManagementEngineStandard/FileManager/ICSVDataReader.cs`
- `DataManagementEngineStandard/FileManager/TextFieldParser.cs`
- `DataManagementEngineStandard/FileManager/CSVTypeMapper.cs`
- `DataManagementEngineStandard/FileManager/CSVAnalyser.cs`

---

## Risks / Compatibility
- **Public API stability**: keep method signatures the same; add new overloads only if needed.
- **Config compatibility**: JSON schema persistence should not silently break existing `Config/*` expectations.
- **Atomic writes**: must handle locked files and avoid partial writes (temp file + replace).
- **Performance regressions**: ensure streaming doesn’t break existing `PagedResult` behavior (count/total rows semantics).

## Acceptance Criteria (Definition of Done)
- JSON:
  - CRUD operations persist after restart.
  - Filtering results consistent across `GetEntity` and `RunQuery`.
  - Paging works on large files without loading everything.
- CSV:
  - Quoted fields containing delimiter/newline are read correctly.
  - Paging does not require reading all rows.
  - Export produces correct CSV and matches the selected entity.

---

## TODO Checklist (Quick)
- [ ] JSON: implement atomic save on `Commit` (and/or per CRUD op)
- [ ] JSON: wire `GetEntityAsync` to async helper
- [ ] JSON: align filtering via filter helper
- [ ] CSV: use `TextFieldParser`/`ICSVDataReader` in `GetEntity`
- [ ] CSV: implement streaming paging
- [ ] CSV: fix `ExportDataToCSV`
- [ ] Add targeted tests or integration checks in `tests/`

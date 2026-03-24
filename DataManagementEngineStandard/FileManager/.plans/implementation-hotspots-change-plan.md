# FileManager Implementation Hotspots Change Plan

This document captures exact planned code changes from the audited FileManager hotspots.

## 1) `CSVDataSource.GetFieldsbyTableScan(...)` loop and inference stability

### Current risk
- Loop control uses mixed variables (`i`/`findex`) and fragile condition logic.
- Broad catches hide inference failures and create non-deterministic field typing.

### Exact change
- Correct loop counters and stop conditions.
- Split inference into deterministic pass helpers (`TryInferType`, `UpdateSizeStats`).
- Emit typed inference diagnostics for skipped/failed rows.

## 2) `CSVDataSource.GetEntity(...)` and paged `GetEntity(...)` header mapping correctness

### Current risk
- `FirstOrDefault(...).Key` can default to `0` when mapping is missing, producing wrong column reads.

### Exact change
- Replace reverse lookups with explicit `entityIndex -> csvIndex` map.
- Add `TryResolveCsvIndex(...)` helper and fail-safe branch for missing mapping.
- Add tests for reordered headers and partial/missing columns.

## 3) `CSVDataSource.GetEntityStructure(...)` / `GetEntityType(...)` empty-list guards

### Current risk
- Patterns like `Entities.Count == 0` followed by `Entities[0]` can throw.

### Exact change
- Introduce `EnsureEntityMetadataLoaded()` helper.
- Replace all direct index assumptions with guard-based flow.
- Ensure refresh path does not dereference empty collections.

## 4) `CSVDataReader.GetName/GetOrdinal/GetValue` projection invariants

### Current risk
- Name and ordinal behavior can diverge under projected columns.

### Exact change
- Normalize projected column metadata once at header read.
- Make `GetName`, `GetOrdinal`, and `GetValue` operate on the same projection map.
- Throw explicit exceptions for unknown ordinals/names instead of implicit failures.

## 5) `CSVTypeMapper.ConvertValue(...)` conversion hardening

### Current risk
- Enum parsing can throw; culture-dependent numeric/date parsing is implicit.

### Exact change
- Use safe enum parse path (`TryParse`) with fallback.
- Introduce optional parse culture/options and consistent fallback semantics.
- Align conversion behavior across datasource and reader paths.

## 6) `CSVAnalyser.AnalyzeCSVFile(...)` heuristic quality

### Current risk
- Quoting checks and uniqueness heuristics can misclassify edge cases.

### Exact change
- Move quote-anomaly checks to parser-level signals where possible.
- Guard uniqueness ratio denominator and add confidence scores.
- Return structured per-column inference rationale.

## 7) `CSVDataSource.ValidateCSVHeaders(...)` header parsing safety

### Current risk
- Uses `Split(Delimiter)` which fails with quoted delimiters.

### Exact change
- Parse header with `CsvTextFieldParser` instead of string split.
- Compare normalized names against both `FieldName` and `Originalfieldname`.
- Emit missing/extra header diagnostics with severity.

## 8) `CSVDataSource` mutation memory patterns (`InsertEntity`, `BulkInsert`)

### Current risk
- Full-file reads/writes increase memory pressure and latency on large files.

### Exact change
- Use streaming append when schema is compatible and file exists.
- Use temp-file streaming rewrite only when required.
- Add size guardrails and operation metrics.

## 9) `CSVDataSource` transaction semantics (`BeginTransaction/Commit/Rollback`)

### Current risk
- Current behavior is snapshot flagging, not true atomic transaction semantics.

### Exact change
- Either (A) document and rename behavior to snapshot scope, or (B) implement staged write set.
- Ensure commit/rollback semantics match API expectations.
- Add explicit test cases for rollback after multi-step mutations.

## 10) `TextFieldParser` responsibility split

### Current risk
- Parser contains file writing utility logic, blurring boundaries.

### Exact change
- Move write/export logic to datasource/helper class.
- Keep parser focused on read/parse concerns.
- Introduce parser options DTO reused by datasource and reader.

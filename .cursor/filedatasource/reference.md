# FileDataSource Extension Reference

## 1) Extension Checklist

- Define target `DataSourceType` for the format.
- Implement `IFileFormatReader` with deterministic row contract.
- Add `[FileReader(DataSourceType.X, "Label", "ext")]` on the reader class.
- Support `Configure(IConnectionProperties)` for format options.
- Implement `ReadHeaders` and `GetEntityStructure` for schema bootstrap.
- Implement `ReadRows` with `ParseMode` + `RowDiagnostic` behavior.
- Implement `CreateFile`, `AppendRow`, `RewriteFile`.
- Register reader using `FileReaderFactory.Register(...)`.
- Verify `FileDataSource.Openconnection()` resolves your reader.
- Verify `GetEntityStructure`, `GetEntity`, update/delete workflows.

## 2) Reader Contract Notes

### `SupportedType`
`FileReaderFactory` stores readers in a dictionary keyed by `DataSourceType`. You can register **many** readers with **different** `SupportedType` values at once. Registering again for the **same** `SupportedType` **replaces** the previous reader for that key (see `FileReaderFactory.Register`).

### `ReadRows`
- Must skip header row only when `HasHeader == true`.
- Must keep column order aligned with schema/header.
- In lenient mode, continue after malformed rows and add diagnostics.
- In strict mode, throw with row context.

### `GetEntityStructure`
- Return `null` for missing file.
- Handle empty file safely.
- For header-less files, infer column count from first data row and generate names (`column1..N` pattern).

### Write methods
- `CreateFile`: initialize file with header row.
- `AppendRow`: add one row while preserving format/escaping.
- `RewriteFile`: use temp file + atomic replace/move pattern.

## 3) Registration Patterns

### App startup registration
```csharp
FileReaderFactory.RegisterDefaults();
FileReaderFactory.Register(new YourCustomReader());
```

### Registry discovery (descriptor-based)
```csharp
using TheTechIdea.Beep.FileManager.Registry;

var registry = new FileReaderRegistry(editor);
registry.Discover(); // finds classes decorated with [FileReader(...)]

var reader = registry.Create(DataSourceType.CSV);
```

### Attribute example
```csharp
using TheTechIdea.Beep.FileManager.Attributes;

[FileReader(DataSourceType.CSV, "CSV", "csv")]
public sealed class CsvFileReader : IFileFormatReader
{
    public DataSourceType SupportedType => DataSourceType.CSV;
    public string GetDefaultExtension() => "csv";
    // ...
}
```

### Test registration
```csharp
FileReaderFactory.Reset(); // internal test-only helper
FileReaderFactory.Register(new YourCustomReader());
```

## 4) Validation Matrix

- **Connection**
  - `Openconnection()` returns `ConnectionState.Open`
  - `_reader` resolved correctly for datasource type
- **Schema**
  - headers read correctly
  - normalized + original field names are preserved as expected
- **Read**
  - row counts are stable
  - diagnostics are populated correctly in bad-data cases
- **Write**
  - append and rewrite do not corrupt existing content
  - escaping/quoting is symmetric with read behavior

## 5) Integration Boundaries

- Keep classification, policy, and governance hooks in `FileDataSource` partials.
- Keep file-format parsing/writing internals in reader classes.
- Do not hardcode extension-specific behavior in `FileDataSource` switch blocks.

## 6) Troubleshooting

- **Reader not used**: ensure registration happens before `Openconnection()`.
- **Reader not discovered**: ensure class has `[FileReader(...)]` and assembly is loaded before `registry.Discover()`.
- **Wrong parser selected**: check `DatasourceType` and `SupportedType`.
- **Header mismatch**: verify `HasHeader` and delimiter/config values.
- **Malformed row crashes in lenient mode**: ensure exceptions are caught in `ReadRows`.
- **Update/Delete corruption**: ensure `RewriteFile` uses atomic replace semantics.

## 7) DuckDB-native formats (Parquet, CSV via DuckDB, JSON, NDJSON, Arrow, …)

When a format should be implemented **using DuckDB’s built-in readers** (`read_parquet`, `read_csv_auto`, `read_json`, `read_ndjson`, …) instead of hand-rolled parsing, use one `IFileFormatReader` **per format** and map to existing `DataSourceType` values such as `Parquet`, `CSV`, `TSV`, `Json`, `Feather`, `Avro`, `ORC`, `Xls` where applicable.

**Catalog and phased plan (separate repo):** `BeepDataSources/InMemoryDB/DuckDBDataSourceCore/.plans/` — start with `00-duckdb-native-file-readers-catalog.md` and `README.md`.

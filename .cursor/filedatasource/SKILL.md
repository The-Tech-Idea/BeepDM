---
name: filedatasource
description: Guidance for using FileDataSource and creating custom file reader extensions in BeepDM. Use when implementing or extending DataManagementEngineStandard/FileManager with new formats via IFileFormatReader and FileReaderFactory.
---

# FileDataSource Guide

Use this skill when working on `DataManagementEngineStandard/FileManager` to either:
- use `FileDataSource` correctly as an `IDataSource`, or
- add a new file-format reader extension.

## Use this skill when
- Implementing or fixing `FileDataSource` behavior (connection, schema, query, CRUD).
- Adding support for a new file type via `IFileFormatReader`.
- Registering custom readers in `FileReaderFactory`.
- Debugging format-specific parse behavior, diagnostics, or schema inference.

## Do not use this skill when
- The task is only generic `IDataSource` behavior. Use [`idatasource`](../idatasource/SKILL.md).
- The task is only connection config modeling. Use [`connectionproperties`](../connectionproperties/SKILL.md) and [`connection`](../connection/SKILL.md).
- The task is primarily ingestion orchestration policies. Use [`importing`](../importing/SKILL.md).

## Responsibilities
- Keep `FileDataSource` format-agnostic and delegate parsing/writing to `_reader`.
- Implement reader-specific logic only in `IFileFormatReader` implementations.
- Keep schema inference and row parsing consistent with existing file readers.
- Preserve `IDataSource` contract behavior (`ErrorObject`, `ConnectionStatus`, `Entities`, `EntitiesNames`).

## Core API Surface
- `FileDataSource`:
  - `Openconnection()`, `Closeconnection()`
  - `GetEntityStructure(...)`, `GetEntity(...)`, CRUD methods
  - `ResolveEntityFilePath(...)`
- Reader contract:
  - `IFileFormatReader` (`Configure`, `ReadHeaders`, `ReadRows`, `GetEntityStructure`, write methods)
- Reader registry:
  - `FileReaderFactory.Register(...)`
  - `FileReaderFactory.RegisterDefaults()`
  - `FileReaderFactory.GetReader(...)`

## Typical Usage Pattern
1. Configure datasource connection (`FilePath`, `FileName`, delimiter/props).
2. Ensure the reader type is registered for the target `DataSourceType`.
3. Call `Openconnection()` so `FileDataSource` resolves `_reader` via `FileReaderFactory`.
4. Use `GetEntityStructure`/`GetEntity`/CRUD through `FileDataSource`.
5. For new formats, implement `IFileFormatReader`, register it, then use `FileDataSource` unchanged.

## Creating a Reader Extension
1. Create a class under `FileManager/Readers` implementing `IFileFormatReader`.
2. Decorate the class with `FileReaderAttribute` so `FileReaderRegistry.Discover()` can build a `FileReaderDescriptor`.
   - Example:
```csharp
[FileReader(DataSourceType.CSV, "CSV", "csv")]
public sealed class CsvFileReader : IFileFormatReader
{
    public DataSourceType SupportedType => DataSourceType.CSV;
    public string GetDefaultExtension() => "csv";
    // ... implement contract
}
```
3. Set `SupportedType` and `GetDefaultExtension()` for the format. Keep them consistent with the attribute.
4. Implement `Configure(IConnectionProperties)` for delimiter/encoding/flags.
5. Implement schema (`ReadHeaders`, `GetEntityStructure`) and row streaming (`ReadRows`).
6. Implement write operations (`CreateFile`, `AppendRow`, `RewriteFile`).
7. Register the reader:
   - Static path: `FileReaderFactory.Register(new YourReader())`
   - Discovery path: ensure assembly is loaded, then run `new FileReaderRegistry(editor).Discover()`.

## Validation and Safety
- Honor `ParseMode` (`Strict` vs `Lenient`) and populate `LastDiagnostics`.
- Keep row streaming forward-only (`IEnumerable<string[]>`) for large files.
- Use atomic rewrite semantics for update/delete style operations.
- Normalize/retain source column names consistently with `FileReaderEntityHelper`.
- Avoid embedding format-specific logic in `FileDataSource` partials.
- If using registry discovery, do not omit `[FileReader(...)]`; without it, the reader will not produce a `FileReaderDescriptor`.

## Pitfalls
- Forgetting to register the reader before `Openconnection()`.
- Returning inconsistent column counts from `ReadRows`.
- Not handling empty files and header-less files deterministically.
- Throwing on every malformed row when `ParseMode.Lenient` is selected.
- Breaking `Entities`/`EntitiesNames` refresh after schema inference.

## File Locations
- `DataManagementEngineStandard/FileManager/FileDataSource.cs`
- `DataManagementEngineStandard/FileManager/FileDataSource.Connection.cs`
- `DataManagementEngineStandard/FileManager/FileDataSource.Schema.cs`
- `DataManagementEngineStandard/FileManager/FileReaderFactory.cs`
- `DataManagementEngineStandard/FileManager/Readers/IFileFormatReader.cs`
- `DataManagementEngineStandard/FileManager/Readers/CsvFileReader.cs`

## Example
```csharp
public sealed class NdjsonFileReader : IFileFormatReader
{
    public DataSourceType SupportedType => DataSourceType.Json;
    public bool HasHeader { get; set; } = false;
    public ParseMode ParseMode { get; set; } = ParseMode.Lenient;
    public IReadOnlyList<RowDiagnostic> LastDiagnostics => _diagnostics;
    private readonly List<RowDiagnostic> _diagnostics = new();

    public string GetDefaultExtension() => "ndjson";
    public void Configure(IConnectionProperties props) { }
    public void ClearDiagnostics() => _diagnostics.Clear();
    public string[] ReadHeaders(string filePath) => Array.Empty<string>();
    public EntityStructure GetEntityStructure(string filePath) { /* infer fields */ return null; }
    public IEnumerable<string[]> ReadRows(string filePath) { /* stream json lines */ yield break; }
    public string InferFieldType(string current, string rawValue) => TypeInferenceHelper.Widen(current, rawValue);
    public bool CreateFile(string filePath, IReadOnlyList<string> headers) => true;
    public bool AppendRow(string filePath, IReadOnlyList<string> headers, IReadOnlyList<string> values) => true;
    public bool RewriteFile(string filePath, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows) => true;
}

// Startup/bootstrap
FileReaderFactory.Register(new NdjsonFileReader());
```

## Related Skills
- [`idatasource`](../idatasource/SKILL.md)
- [`connectionproperties`](../connectionproperties/SKILL.md)
- [`importing`](../importing/SKILL.md)
- [`beepdm`](../beepdm/SKILL.md)

## Detailed Reference
Use [`reference.md`](./reference.md) for a complete extension checklist and implementation template.

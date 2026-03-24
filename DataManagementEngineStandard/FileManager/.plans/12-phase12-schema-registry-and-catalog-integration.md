# Phase 12 — Schema Registry and Catalog Integration

| Attribute      | Value                                      |
|----------------|--------------------------------------------|
| Phase          | 12                                         |
| Status         | planned                                    |
| Priority       | High                                       |
| Dependencies   | Phase 3 (schema inference), Phase 11 (ingestion contracts) |
| Est. Effort    | 4 days                                     |

---

## 1. Goal

Give the FileManager a **versioned schema registry** so that:
- Every CSV entity's inferred schema is captured and versioned.
- Schema drift (new columns, type changes, dropped columns) is detected and reported before rows are committed.
- Column-level lineage maps source columns to their target fields.
- Metadata can be exported to external data catalogs (Apache Atlas, Azure Purview, DataHub, custom REST catalog).

---

## 2. Motivation

| Current state | Enterprise requirement |
|---------------|------------------------|
| Schema is inferred fresh on every read | Schema should be registered once and compared on re-ingest |
| No way to know if a column was renamed or dropped between file versions | Schema drift detection before first row is committed |
| No column-level lineage | Each column must carry source→target mapping for data governance |
| No catalog integration | Data stewards must be able to see "what files feed which entities" in a catalog |

---

## 3. Schema Registry Contracts

### 3.1 `IFileSchemaRegistry`

```csharp
namespace TheTechIdea.Beep.FileManager.Schema
{
    /// <summary>
    /// Versioned schema registry for file-based entities.
    /// Each (SourceSystem + EntityName) pair has an ordered list of schema versions.
    /// </summary>
    public interface IFileSchemaRegistry
    {
        /// <summary>
        /// Registers a new schema version for the given key.
        /// If the schema is identical to the latest version, returns the existing version number.
        /// </summary>
        Task<int> RegisterAsync(
            string sourceSystem,
            string entityName,
            FileSchemaVersion schema,
            CancellationToken ct = default);

        /// <summary>Returns the latest registered schema version for the key.</summary>
        Task<FileSchemaVersion> GetLatestAsync(
            string sourceSystem,
            string entityName,
            CancellationToken ct = default);

        /// <summary>Returns a specific version.</summary>
        Task<FileSchemaVersion> GetVersionAsync(
            string sourceSystem,
            string entityName,
            int version,
            CancellationToken ct = default);

        /// <summary>Returns all versions in ascending order.</summary>
        Task<IReadOnlyList<FileSchemaVersion>> GetHistoryAsync(
            string sourceSystem,
            string entityName,
            CancellationToken ct = default);

        /// <summary>
        /// Compares <paramref name="candidate"/> against the latest registered schema.
        /// Returns a diff report — never commits anything.
        /// </summary>
        Task<SchemaDriftReport> DetectDriftAsync(
            string sourceSystem,
            string entityName,
            FileSchemaVersion candidate,
            CancellationToken ct = default);
    }
}
```

### 3.2 `FileSchemaVersion`

```csharp
namespace TheTechIdea.Beep.FileManager.Schema
{
    public sealed class FileSchemaVersion
    {
        public int VersionNumber { get; init; }             // 1-based, auto-assigned
        public DateTimeOffset RegisteredAt { get; init; }
        public string RegisteredBy { get; init; }           // job ID or username
        public string FileChecksum { get; init; }           // checksum of the source file that generated this schema
        public IReadOnlyList<FileColumnSchema> Columns { get; init; }
        public string Delimiter { get; init; }
        public bool HasHeader { get; init; }
        public string Encoding { get; init; }               // "UTF-8", "ISO-8859-1", …
    }

    public sealed class FileColumnSchema
    {
        public int    OrdinalPosition { get; init; }        // 0-based column index in source file
        public string SourceColumnName { get; init; }       // as it appears in the CSV header
        public string TargetFieldName { get; init; }        // mapped destination field name
        public string InferredType { get; init; }           // "string", "int32", "decimal", "datetime", …
        public bool   IsNullable { get; init; }
        public double NullRate { get; init; }               // fraction of rows that were null (0.0–1.0)
        public double UniquenessRatio { get; init; }        // fraction of rows with unique value
        public int    MaxLength { get; init; }              // max observed character length for string fields
        public string SampleValues { get; init; }           // comma-joined first 5 distinct values (for catalog display)
        public IReadOnlyList<string> DataClassifications { get; init; }  // e.g. ["PII-Email", "Confidential"]
    }
}
```

### 3.3 `SchemaDriftReport`

```csharp
namespace TheTechIdea.Beep.FileManager.Schema
{
    public sealed class SchemaDriftReport
    {
        public string SourceSystem { get; init; }
        public string EntityName { get; init; }
        public int BaselineVersion { get; init; }
        public bool HasDrift => AddedColumns.Count > 0 || DroppedColumns.Count > 0 || TypeChanges.Count > 0 || RenamedColumns.Count > 0;
        public bool IsBreaking { get; init; }               // true if drift would cause data loss or type errors

        public IReadOnlyList<FileColumnSchema> AddedColumns { get; init; }
        public IReadOnlyList<FileColumnSchema> DroppedColumns { get; init; }
        public IReadOnlyList<ColumnTypeChange> TypeChanges { get; init; }
        public IReadOnlyList<ColumnRename> RenamedColumns { get; init; }
        public IReadOnlyList<string> Warnings { get; init; }

        /// <summary>Human-readable summary for logs / notifications.</summary>
        public string Summary { get; init; }
    }

    public sealed record ColumnTypeChange(
        string ColumnName,
        string OldType,
        string NewType,
        bool IsWidening);     // int→long = widening (safe); decimal→string = narrowing (risky)

    public sealed record ColumnRename(
        string OldName,
        string NewName,
        double NameSimilarity); // Levenshtein-based confidence score
}
```

---

## 4. Schema Drift Classification

| Drift type | Breaking? | Recommended action |
|------------|-----------|--------------------|
| New nullable column | No | Register new version, log INFO |
| New NOT-NULL column | Yes | Quarantine file, alert steward |
| Dropped column | Depends on target | Check if target field has NOT-NULL constraint |
| Type widening (int→long, float→double) | No | Register new version, log WARN |
| Type narrowing (decimal→string) | Yes | Quarantine file, alert steward |
| Column rename (high confidence) | No | Register with `ColumnRename` record |
| Column rename (low confidence) | Yes | Treat as drop + add, alert steward |
| Reordered columns only | No | Log WARN if ordinal-based reading is in use |
| Encoding change | Yes | Quarantine file |

---

## 5. Column Lineage

Column lineage records the transformation path from source to target:

```csharp
namespace TheTechIdea.Beep.FileManager.Schema
{
    public sealed class ColumnLineageEntry
    {
        public string JobId { get; init; }
        public string SourceSystem { get; init; }
        public string SourceFile { get; init; }
        public int    SourceColumnOrdinal { get; init; }
        public string SourceColumnName { get; init; }
        public string TargetEntity { get; init; }
        public string TargetField { get; init; }
        public string TransformationRule { get; init; }  // e.g. rule key if a rule was applied, null if direct copy
        public DateTimeOffset RecordedAt { get; init; }
    }

    public interface IColumnLineageStore
    {
        Task RecordAsync(IEnumerable<ColumnLineageEntry> entries, CancellationToken ct = default);
        Task<IReadOnlyList<ColumnLineageEntry>> GetLineageForTargetAsync(string targetEntity, string targetField, CancellationToken ct = default);
        Task<IReadOnlyList<ColumnLineageEntry>> GetLineageForSourceAsync(string sourceSystem, string sourceColumnName, CancellationToken ct = default);
    }
}
```

---

## 6. Catalog Export Adapter

```csharp
namespace TheTechIdea.Beep.FileManager.Schema
{
    /// <summary>
    /// Push schema metadata to an external data catalog.
    /// Implement this interface for Azure Purview, DataHub, Apache Atlas, or a custom REST catalog.
    /// </summary>
    public interface ICatalogExportAdapter
    {
        string CatalogName { get; }

        /// <summary>
        /// Upserts the entity metadata (name, schema, owner, tags) into the catalog.
        /// </summary>
        Task UpsertEntityAsync(
            FileSchemaVersion schema,
            string sourceSystem,
            string entityName,
            CancellationToken ct = default);

        /// <summary>
        /// Publishes column-level lineage for a completed ingestion job.
        /// </summary>
        Task PublishLineageAsync(
            IEnumerable<ColumnLineageEntry> lineage,
            CancellationToken ct = default);
    }
}
```

### 6.1 Reference: No-op adapter (for testing / environments without a catalog)

```csharp
public sealed class NullCatalogExportAdapter : ICatalogExportAdapter
{
    public string CatalogName => "NullCatalog";
    public Task UpsertEntityAsync(FileSchemaVersion schema, string sourceSystem, string entityName, CancellationToken ct = default)
        => Task.CompletedTask;
    public Task PublishLineageAsync(IEnumerable<ColumnLineageEntry> lineage, CancellationToken ct = default)
        => Task.CompletedTask;
}
```

---

## 7. Integration with the Ingestion Pipeline (Phase 11)

The schema registry check is inserted between `Validating` and `Ingesting` states:

```
Validating
    ↓
[Get candidate schema from CSVAnalyser]
    ↓
[IFileSchemaRegistry.DetectDriftAsync]
    ├── No drift  →  RegisterAsync (no-op if same)  →  Ingesting
    ├── Non-breaking drift  →  RegisterAsync (new version)  →  Ingesting  (+ WARN log)
    └── Breaking drift  →  Quarantined  (+ notify data steward)
```

---

## 8. Schema Registry Storage Backends

Provide at least two implementations:

| Backend | Class | When to use |
|---------|-------|-------------|
| JSON files on disk | `JsonFileSchemaRegistry` | Local dev, single-node deployments |
| SQLite table | `SqliteSchemaRegistry` | Small-scale production |
| Pluggable SQL (via `IDMEEditor`) | `BeepDbSchemaRegistry` | Full enterprise — use any Beep-supported RDBMS |

All three implement `IFileSchemaRegistry` so callers never depend on the storage backend.

---

## 9. Acceptance Criteria

| # | Criterion | Test |
|---|-----------|------|
| 1 | Same inferred schema registered twice → returns same version number | Unit |
| 2 | Added-column drift detected and `HasDrift = true`, `IsBreaking = false` for nullable column | Unit |
| 3 | Dropped NOT-NULL column → `IsBreaking = true` | Unit |
| 4 | Type narrowing (decimal→string) → `IsBreaking = true` | Unit |
| 5 | `ICatalogExportAdapter.UpsertEntityAsync` called once per completed ingestion job | Integration |
| 6 | `IColumnLineageStore.GetLineageForTargetAsync` returns all source columns for a given target field | Integration |
| 7 | `JsonFileSchemaRegistry` persists and re-loads correctly across process restarts | Integration |

---

## 10. Deliverables

| Artifact | Location |
|----------|----------|
| `Schema/IFileSchemaRegistry.cs` | `FileManager/Schema/` |
| `Schema/FileSchemaVersion.cs` | `FileManager/Schema/` |
| `Schema/SchemaDriftReport.cs` | `FileManager/Schema/` |
| `Schema/IColumnLineageStore.cs` | `FileManager/Schema/` |
| `Schema/ICatalogExportAdapter.cs` | `FileManager/Schema/` |
| `Schema/NullCatalogExportAdapter.cs` | `FileManager/Schema/` |
| `Schema/JsonFileSchemaRegistry.cs` | `FileManager/Schema/Implementations/` |
| `Schema/SqliteSchemaRegistry.cs` | `FileManager/Schema/Implementations/` |
| Unit tests | `tests/FileManager/SchemaRegistryTests.cs` |

---

## 11. Enterprise Standards Traceability

| Standard | Clause | Addressed |
|----------|--------|-----------|
| DAMA-DMBOK v2 | Ch. 13 — Metadata Management | `IFileSchemaRegistry`, catalog export |
| ISO/IEC 11179 | Data element naming & registration | `FileColumnSchema.TargetFieldName` |
| GDPR Art. 30 | Records of processing activities | `IColumnLineageStore` |
| Data Mesh Principle | Data product schema ownership | Per-source versioned schema |

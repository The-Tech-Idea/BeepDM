# File Helper

## Purpose
File-format helper logic for file-backed data sources (CSV, Excel, JSON, XML, Parquet, PDF). Handles schema inference, format-specific parsing, and serialization.

## Key Files
- `FileFormatHelper.cs`: File helper for format-aware behavior, schema inference, delimiter/format detection, and I/O conversions.

## Features
- Schema inference from file headers and content
- Format-specific delimiter/separator detection (CSV, TSV, etc.)
- File extension-based format routing
- Parse and serialization error diagnostics
- Streaming read support for large files

## Usage Notes
- Keep schema inference and delimiter/format behavior deterministic
- Validate file-format assumptions before runtime processing
- Surface parse and serialization errors with actionable diagnostics

## Related Documentation
- [FileManager DataSource Help](../../../Help/filemanager-reader-host.html)
- [Datasource Types Reference](../../../Help/datasource-types-reference.html)

# Utils

General utility helpers reused across the engine.

## Key Files
- `DataConversionHelper.cs`: Type conversion between datasource formats
- `EntityStructureHelper.cs`: Entity metadata extraction and comparison
- `FileDataSourceHelper.cs`: File I/O utilities for file-based datasources
- `FilterHelper.cs`: AppFilter construction and translation
- `ReflectionHelper.cs`: Reflection utilities for dynamic type discovery
- `Util.cs`: Main utility facade implementing IUtil (string manipulation, date handling, formatting)

## How It Fits
Provides common helper operations to reduce duplication in datasource, editor, and helper modules. DMEEditor, ConfigEditor, and all datasource implementations depend on these utilities for consistent behavior across the engine.

## Related Documentation
- [Core Architecture](../Docs/CoreArchitecture.md)

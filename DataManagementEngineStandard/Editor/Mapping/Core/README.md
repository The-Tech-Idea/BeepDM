# Mapping Core

## Purpose
This folder contains the AutoObjMapper engine internals: configuration application, expression generation, factory presets, validation wrappers, and performance metrics.

## Key Files
- `AutoObjMapper.Core.cs`: Mapper core with configuration and statistics access.
- `AutoObjMapper.ExpressionBuilder.cs`: Expression-tree pipeline for compiled mappings.
- `AutoObjMapper.Factory.cs`: Factory presets (`CreateDefault`, `CreateHighPerformance`, `CreateDiagnostic`) and fluent options builder.
- `AutoObjMapper.Performance.cs`: Performance metric collection/reset support.
- `AutoObjMapper.Validation.cs`: `MappingResult` success/failure wrappers.

## Runtime Flow
1. Build mapper options and type-map configuration.
2. Compile mapping delegates through expression builders.
3. Execute mapping with validation and optional diagnostics.
4. Read or clear metrics using statistics/performance APIs.

## Extension Guidelines
- Keep compiled expression caching deterministic.
- Guard deep object traversal with max-depth options.
- Preserve behavior parity between default and diagnostic factories.

## Testing Focus
- Mapping correctness for nested objects and nullable conversions.
- Performance regressions in hot mapping paths.
- Error/warning behavior in `MappingResult` outputs.

# Mapping Helpers

## Purpose
This folder contains supporting helpers for mapping defaults, validation, reflection-based discovery, type conversion, and performance instrumentation.

## Key Files
- `MappingDefaultsHelper.cs`: Applies configured defaults to destination objects.
- `MappingValidationHelper.cs`: Type and instance compatibility validation.
- `PropertyDiscoveryHelper.cs`: Read/write property introspection and mapping metadata.
- `TypeConversionHelper.cs`: Safe conversion utility for mismatched property types.
- `MappingPerformanceHelper.cs`: Metrics and monitored execution wrappers.

## Runtime Flow
1. Discover source/destination properties.
2. Validate map compatibility.
3. Convert values and apply destination defaults.
4. Record timing and diagnostics when monitoring is enabled.

## Extension Guidelines
- Cache reflection results where possible.
- Keep conversion rules predictable and culture-safe.
- Treat validation warnings separately from hard errors.

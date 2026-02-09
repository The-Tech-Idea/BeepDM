# Mapping Interfaces

## Purpose
This folder defines the contracts for the AutoObjMapper pipeline, including mapper execution, configuration registration, and type-map behavior.

## Key Interfaces
- `IAutoObjMapper`: Main mapping operations and mapper lifecycle.
- `IMapperConfiguration`: Registry of source/destination type maps.
- `ITypeMapConfiguration`: Fluent per-map behavior configuration.
- `ITypeMapBase`: Common type-map metadata and resolver contract.
- `ITypeMapConfig` (from `ITypeMapConfig.cs`): Shared configuration model contract.

## Integration Notes
- Keep configuration and execution interfaces separated for testability.
- Preserve fluent configuration ergonomics when evolving contracts.
- Ensure interface additions do not break existing mapper factories.

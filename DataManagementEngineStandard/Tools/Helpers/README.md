# Tools Helpers

## Purpose
This folder provides code-generation helpers that convert `EntityStructure` metadata into concrete artifacts such as POCOs, EF Core layers, APIs, validators, and UI components.

## Key Files
- `ClassGenerationHelper.cs`: Core naming, type mapping, and structural validation utilities.
- `PocoClassGeneratorHelper.cs` and `ModernClassGeneratorHelper.cs`: Domain class and modern C# model generation.
- `DatabaseClassGeneratorHelper.cs`: EF Core context, repository, and migration scaffolding.
- `WebApiGeneratorHelper.cs` and `UiComponentGeneratorHelper.cs`: API endpoints and UI artifact generation.
- `ValidationAndTestingGeneratorHelper.cs`: Unit test and validator class generation.
- `DocumentationGeneratorHelper.cs`: Entity-level documentation and diff report generation.
- `PocoToEntityGeneratorHelper.cs`: Reverse mapping from CLR models back to `EntityStructure` metadata.

## Runtime Flow
1. Validate entity metadata (`ValidateEntityStructure`).
2. Select generator path (POCO, API, EF, UI, tests, docs).
3. Emit files to target output folder with consistent naming conventions.

## Extension Guidelines
- Keep generated code deterministic to avoid noisy diffs.
- Reuse `ClassGenerationHelper` naming and type-conversion utilities across generators.
- Validate generated source with compile checks for representative entities.

## Testing Focus
- Generation for entities with composite keys, nullable fields, and relationships.
- Name sanitization for invalid identifiers.
- Backward compatibility of generated public APIs.

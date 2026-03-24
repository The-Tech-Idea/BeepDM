# EF Core Decorators to Entity Enhancement Plan

## Goal

Add a clear, maintainable enhancement path for converting EF Core decorated classes into:

- `EntityStructure` metadata
- Generated Beep `Entity` classes

without modifying model-layer types under `DataManagementModelsStandard`.

## New Directives

1. EF conversion must live in a **separate dedicated class** (not mixed into general POCO helper logic).
2. Conversion entry points must accept **Type**, **file path**, **raw source string**, and **directory** inputs.
3. Add the **reverse direction**: generate EF Core classes from `List<EntityStructure>` to C# files and optional DLL output.

## Scope

- In scope:
  - New dedicated EF conversion helper class under `DataManagementEngineStandard/Tools/Helpers/`
    - proposed: `EfCoreToEntityGeneratorHelper.cs`
  - `DataManagementEngineStandard/Tools/Helpers/PocoToEntityGeneratorHelper.cs` (only as bridge/delegator if needed)
  - `DataManagementEngineStandard/Tools/ClassCreator.PocoToEntity.cs`
  - `DataManagementModelsStandard/Tools/IClassCreator.cs` (API surface only)
- Out of scope:
  - Any behavior change in `DataManagementModelsStandard/Editor/Entity.cs`
  - Any behavior change in `DataManagementModelsStandard/DataBase/EntityStructure.cs`

## Current State

- POCO-to-Entity conversion exists and already reads many DataAnnotation decorators.
- EF-focused API methods are partially mixed with POCO methods.
- There is no dedicated, explicit API contract that clearly separates:
  - generic POCO conversion
  - EF Core decorated conversion

## Target State

Provide explicit EF conversion APIs and a predictable workflow:

1. Discover EF classes from multiple sources:
   - CLR types / namespace scan
   - C# file
   - C# source string
   - directory of C# files
2. Convert to metadata (`EntityStructure`) using EF decorators
3. Generate classes (`Entity` code/files)
4. Expose these entry points through `IClassCreator` and `ClassCreator`

Also provide the reverse workflow:

1. Accept `List<EntityStructure>`
2. Generate EF Core model classes with decorators
3. Emit output as:
   - per-class C# files
   - single combined C# file
   - compiled DLL (optional)

## Functional Requirements

EF decorator support must map as follows:

- Class-level:
  - `[Table(Name, Schema = ...)]` -> `EntityName`, `DatasourceEntityName`, `SchemaOrOwnerOrDatabase`
- Property-level:
  - `[Column(Name, TypeName = ...)]` -> `ColumnName`, `ColumnTypeName`
  - `[Key]` -> `IsKey`, `PrimaryKeys`
  - `[Required]` -> `IsRequired`, `AllowDBNull = false`
  - `[MaxLength]`, `[StringLength]` -> `Size1`, `MaxLength`, `ValueMin`
  - `[DatabaseGenerated(...)]` -> `DatabaseGeneratedOptionName`, `IsAutoIncrement`
  - `[NotMapped]` -> excluded from generated fields
  - `[ForeignKey]` + navigation conventions -> `Relations`

Input modes must support:

- `Type` / `IEnumerable<Type>`
- `string filePath` (single `.cs` file)
- `string sourceCode` (raw C# content)
- `string directoryPath` (+ recursive option)

Reverse generation modes must support:

- `List<EntityStructure>` -> EF C# files (one file per entity)
- `List<EntityStructure>` -> single EF C# file
- `List<EntityStructure>` -> DLL via generated EF model sources

## Non-Functional Requirements

- Keep backward compatibility for existing POCO APIs.
- Keep runtime type generation unchanged unless using new EF methods.
- No dependence on model-layer extensions or static helpers.
- Keep methods testable with pure reflection inputs.

## Proposed Implementation Phases

## Phase 1 - API Clarification

- Create a dedicated helper:
  - `EfCoreToEntityGeneratorHelper`
- Move/implement EF-specific logic there:
  - `ConvertEfCoreTypeToEntityStructure(...)`
  - `ConvertEfCoreTypesToEntityStructures(...)`
  - `ScanNamespaceForEfCoreClasses(...)`
  - `GenerateEntityClassFromEfCore(...)`
  - `GenerateEntityClassesFromEfCoreNamespace(...)`
- Add file/string/directory conversion methods:
  - `ConvertEfCoreFileToEntityStructures(string filePath, ...)`
  - `ConvertEfCoreSourceToEntityStructures(string sourceCode, ...)`
  - `ConvertEfCoreDirectoryToEntityStructures(string directoryPath, bool recursive = true, ...)`
  - `GenerateEntityClassesFromEfCoreFile(...)`
  - `GenerateEntityClassesFromEfCoreDirectory(...)`
- Add reverse-generation methods:
  - `GenerateEfCoreClassesFromEntityStructures(List<EntityStructure> entities, string outputPath, ...)`
  - `GenerateEfCoreCombinedFileFromEntityStructures(List<EntityStructure> entities, string outputFilePath, ...)`
  - `GenerateEfCoreDllFromEntityStructures(List<EntityStructure> entities, string dllName, string outputPath, ...)`
- Keep POCO methods unchanged and isolated.

Deliverable:
- Dedicated EF helper exists with explicit EF + file/string/directory entry points.

## Phase 2 - Interface and Facade Wiring

- Add matching signatures to `IClassCreator`.
- Implement pass-through wrappers in `ClassCreator.PocoToEntity.cs` to delegate to helper.
- Ensure naming is consistent and discoverable from IntelliSense.
- Keep backward compatibility by preserving old methods and adding new overloads.

Additional wiring for reverse generation:

- Expose `List<EntityStructure>` -> EF class/file/DLL methods in `IClassCreator`.
- Implement wrappers in `ClassCreator` that delegate to dedicated EF helper.

Deliverable:
- Consumers can call EF conversion directly via `ClassCreator`/`IClassCreator`.

## Phase 3 - Relationship Accuracy Hardening

- Improve `ForeignKey` + navigation resolution:
  - scalar FK with `[ForeignKey("Nav")]`
  - navigation with `[ForeignKey("FkField")]`
  - default `{RelatedType}Id` fallback
- Ensure no duplicate relations are added.

Deliverable:
- Stable relation mapping across common EF class patterns.

## Phase 4 - Code Generation Consistency

- Ensure generated Entity classes preserve mapped decorator intent:
  - include emitted decorators in generated class where applicable
  - keep scalar-only output mode default (no navigation property emission)
- Validate generated naming and nullability output.

Deliverable:
- Generated entity code aligns with EF mapping metadata.

Reverse-generation consistency:

- Ensure generated EF classes from `EntityStructure` emit expected decorators:
  - `[Table]`, `[Key]`, `[Required]`, `[MaxLength]/[StringLength]`, `[Column]`, `[DatabaseGenerated]`, `[NotMapped]`
- Ensure type/nullability mapping from `EntityField` to C# EF properties is stable.

Deliverable:
- Generated EF model code aligns with `EntityStructure` metadata.

## Phase 5 - Validation and Regression Safety

- Add/execute test matrix with representative EF models:
  - simple key
  - composite key hints (where represented)
  - required + max length
  - table/schema mapping
  - fk + navigation pairs
  - not mapped properties
- Confirm POCO conversion behavior is unchanged.

Deliverable:
- Verified EF path + no regressions to existing POCO path.

## Acceptance Criteria

- EF-decorated class conversion callable through `IClassCreator` and `ClassCreator`.
- EF conversion logic is hosted in a dedicated helper class (not mixed with generic POCO internals).
- Output `EntityStructure` contains expected field metadata for decorators listed above.
- Generated entity class output matches metadata mapping.
- No changes required in `DataManagementModelsStandard` model classes.
- Existing POCO conversion methods continue to work as before.
- File/string/directory conversion paths produce equivalent metadata output to Type-based conversion.
- Reverse generation is available:
  - `List<EntityStructure>` -> EF C# files
  - `List<EntityStructure>` -> combined EF C# file
  - `List<EntityStructure>` -> EF DLL

## Risks and Mitigations

- Risk: EF decorator edge cases differ across projects.
  - Mitigation: add focused normalization helpers and fallback conventions.
- Risk: accidental behavior drift in existing POCO path.
  - Mitigation: keep EF methods additive, reuse existing internals carefully.
- Risk: duplicate relation creation.
  - Mitigation: relation de-dup logic keyed by FK + related entity.

## Rollout Strategy

1. Ship helper methods first.
2. Add interface/facade methods.
3. Run conversion/regression tests.
4. Document new EF-specific usage examples.

## Example Usage (Target)

```csharp
var creator = new ClassCreator(editor);

// Discover EF classes in namespace
var efTypes = creator.ScanNamespaceForEfCoreClasses("MyApp.Domain.Models");

// Convert to metadata
var entities = creator.ConvertEfCoreTypesToEntityStructures(efTypes, includeRelationships: true);

// Generate classes
var code = creator.GenerateEntityClassesFromEfCoreNamespace(
    "MyApp.Domain.Models",
    outputPath: @"C:\out\entities",
    targetNamespace: "MyApp.Generated.Entities",
    generateFiles: true,
    assembly: null,
    includeRelationships: true);

// Convert from one EF source file
var entitiesFromFile = creator.ConvertEfCoreFileToEntityStructures(
    @"C:\models\OrderModels.cs",
    includeRelationships: true);

// Convert from raw C# source text
var entitiesFromText = creator.ConvertEfCoreSourceToEntityStructures(
    efSourceCode,
    includeRelationships: true);

// Convert from a folder of model files
var entitiesFromDir = creator.ConvertEfCoreDirectoryToEntityStructures(
    @"C:\models",
    recursive: true,
    includeRelationships: true);

// Reverse: generate EF classes from EntityStructure list
var efFiles = creator.GenerateEfCoreClassesFromEntityStructures(
    entitiesFromDir,
    outputPath: @"C:\out\ef-models",
    namespaceName: "MyApp.Generated.EF");

var efCombined = creator.GenerateEfCoreCombinedFileFromEntityStructures(
    entitiesFromDir,
    outputFilePath: @"C:\out\ef-models\GeneratedEfModels.cs",
    namespaceName: "MyApp.Generated.EF");

var efDll = creator.GenerateEfCoreDllFromEntityStructures(
    entitiesFromDir,
    dllName: "MyApp.Generated.EF.Models",
    outputPath: @"C:\out\ef-models\bin",
    namespaceName: "MyApp.Generated.EF");
```


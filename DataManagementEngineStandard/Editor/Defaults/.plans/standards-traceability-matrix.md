# DefaultsManager Standards Traceability Matrix

## Purpose
Map requested and enterprise-aligned enhancements to concrete DefaultsManager modules and phase documents.

| Requirement/Standard Theme | Phase | Primary Files |
|---|---|---|
| Simple expression language (`Operator.value1.value2`) | 1, 3 | `Resolvers/ExpressionResolver.cs`, `Resolvers/FormulaResolver.cs`, `Helpers/DefaultValueValidationHelper.cs` |
| Query-based defaults enhancements | 2 | `Resolvers/DataSourceResolver.cs`, `Resolvers/BaseDefaultValueResolver.cs` |
| Backward compatibility for existing rules | 1, 7 | `DefaultsManager.cs`, `Resolvers/DefaultValueResolverManager.cs` |
| Safer rule execution and validation | 2, 5 | `Helpers/DefaultValueValidationHelper.cs`, `DefaultsManager.cs` |
| Resolver extensibility for future types | 4 | `Interfaces/IDefaultValueInterfaces.cs`, `Resolvers/DefaultValueResolverManager.cs` |
| Performance and deterministic behavior | 6 | `Resolvers/DefaultValueResolverManager.cs`, `DefaultsManager.Extended.cs` |
| Migration and operational adoption | 7 | `README_DefaultsManager.md`, `Examples/DefaultsManagerExamples.cs` |

## Requested New Default Types Coverage
- Query defaults:
  - scalar query result
  - first-row field lookup
  - existence check
  - aggregate result
- Expression defaults:
  - dot-style operators (`ADD.10.5`, `IF.GTE.Age.18.Adult.Minor`)
  - function-style aliases remain supported

## Traceability Rule
- Any implementation PR under `Defaults` must reference:
  - at least one phase document (`01`-`07`),
  - and one row from this matrix.

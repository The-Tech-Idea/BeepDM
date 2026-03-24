# DefaultsManager Enhancement Program - Overview and Gap Matrix

## Objective
Define a phased enhancement roadmap for `DefaultsManager` to support modern, safe, and expressive default-value computation, including query-driven defaults and a simplified expression DSL.

## Baseline Architecture
- Core manager: `DefaultsManager.cs`, `DefaultsManager.Extended.cs`, `DefaultsManager.Templates.cs`
- Resolver orchestration: `Resolvers/DefaultValueResolverManager.cs`
- Validation: `Helpers/DefaultValueValidationHelper.cs`
- Existing dynamic resolvers:
  - `Resolvers/DataSourceResolver.cs` (query/getscalar/lookup/aggregate)
  - `Resolvers/ExpressionResolver.cs` (if/case/coalesce/eval)
  - `Resolvers/FormulaResolver.cs` (math/sequence/random)
  - plus environment/config/system/user/property resolvers

## Current Gaps (High Signal)
- Rule language is inconsistent across resolvers (function-like syntax only, different semantics per resolver).
- Query-based defaults exist but need stronger safety controls (parameterization, allowlist, timeout, result shape).
- Expression handling is basic and split across `ExpressionResolver` and `FormulaResolver`.
- No formal grammar/versioning strategy for rule syntax.
- Limited operational controls for caching, diagnostics, and deterministic testability.

## Requested Direction
- Add new defaults using query and expressions.
- Add a simple expression language style like `Operator.value1.value2`.

## Gap Matrix

| Capability | Current | Target | Priority |
|---|---|---|---|
| Rule DSL | Function syntax only (`IF(...)`, `QUERY(...)`) | Dual syntax: existing + simple dot DSL (`Operator.value1.value2`) with versioning | P0 |
| Query Defaults | Supported in `DataSourceResolver` | Secure query templates, parameter binding, policy checks, deterministic fallback | P0 |
| Expression Defaults | Supported but basic parser/evaluator | Unified expression engine with typed operators and extensible functions | P0 |
| Validation | Rule validation exists | Grammar-aware compile-time validation and diagnostics catalog | P1 |
| Observability | Logs exist | Resolver-level metrics, latency/error counters, troubleshooting context IDs | P1 |
| Performance | No shared cache policy | Resolver/query cache strategy and expression pre-compilation | P2 |
| Migration | Backward compatibility implicit | Explicit compatibility mode and migration linter/autofix rules | P1 |

## Proposed Phase Sequence
1. Rule Language and Compatibility Model
2. Query Default Enhancements
3. Expression and Formula Unification
4. Resolver Extensibility and Context Model
5. Validation, Safety, and Observability
6. Performance and Caching
7. Migration, DevEx, and Rollout

## Success Criteria
- New DSL supports both `Operator.value1.value2` and current function syntax.
- Query defaults are safer and policy-governed.
- Expression behavior is deterministic and testable.
- Existing defaults continue to run under compatibility mode with migration guidance.

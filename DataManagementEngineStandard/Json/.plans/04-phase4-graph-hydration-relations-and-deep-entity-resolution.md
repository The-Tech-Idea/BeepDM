# Phase 4 - Graph Hydration, Relations, and Deep Entity Resolution

## Objective
Make deep object graph materialization predictable, safe, and configurable.

## Scope
- Relation traversal and recursion controls.
- Graph hydration options and cycle handling.
- Deep entity resolution correctness.

## File Targets
- `Json/Helpers/JsonGraphHelper.cs`
- `Json/Helpers/JsonRelationHelper.cs`
- `Json/Helpers/JsonDeepEntityResolver.cs`
- `Json/Helpers/GraphHydrationOptions.cs`

## Planned Enhancements
- Depth limits and cycle-detection policies.
- Reference reuse/merge policies for repeated nodes.
- Consistent relation join semantics across nested collections.

## Acceptance Criteria
- Deep hydration works within configured limits.
- Cycles and repeated references are handled without runaway recursion.
- Result shape remains deterministic and test-covered.

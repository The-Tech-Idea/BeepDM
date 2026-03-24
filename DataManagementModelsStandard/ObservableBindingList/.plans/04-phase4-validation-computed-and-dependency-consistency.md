# Phase 4 - Validation, Computed, and Dependency Consistency

## Objective
Ensure computed fields and validation flows remain consistent under incremental changes.

## Audited Hotspots
- `ObservableBindingList.Validation.cs`: `Validate`, `ValidateProperty`, annotation/custom cache paths.
- `ObservableBindingList.Computed.cs`: compute registration/cache invalidation.
- `ObservableBindingList.Aggregates.cs`: aggregate/group compute over mutable list state.

## File Targets
- `ObservableBindingList.Validation.cs`
- `ValidationResult.cs`
- `ObservableBindingList.Computed.cs`
- `ObservableBindingList.Aggregates.cs`

## Real Constraints to Address
- Cache eviction and recompute ordering are not uniformly enforced after all mutation paths.
- Validation and computed refresh can drift when batched operations suppress notifications.
- Aggregate behavior must be deterministic with paging/filter states.

## Acceptance Criteria
- Dependency recompute order is deterministic.
- Validation errors are structured and stable.
- Computed/aggregate outputs are consistent after complex edits.

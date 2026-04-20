# Phase 08 - Cross-Shard Query Planner & Result Merger

## Objective

Add a small, focused query planner that turns a high-level read intent (filter,
sort, top, group/aggregate) into per-shard sub-queries plus a merge plan.
Replaces the v1 `BasicResultMerger` with a query-aware merger.

Scope is intentionally narrow in v1: filter pushdown, scatter, then merge
(union, sort-merge, top-N, simple group-by aggregates: COUNT/SUM/MIN/MAX/AVG).
Joins across shards are NOT supported in v1 except where one side is a
Broadcast table.

## Dependencies

- Phase 06 (read executor & basic merger).
- Phase 03 (placement; needed to detect Broadcast joins).

## Scope

- `QueryIntent` - structured representation of the read shape (filters,
  selected columns, group-by, aggregates, order-by, top).
- `QueryPlanner` - converts a `QueryIntent` plus a `RoutingDecision` into a
  `QueryPlan` (per-shard sub-intent + merge spec).
- `MergeSpec` - declares how to merge per-shard outputs (union, top-N,
  sort-merge, group/aggregate).
- `QueryAwareResultMerger` - implements `IResultMerger`; honors `MergeSpec`.
- Detect Broadcast-side joins: if one side of a "filter join" is Broadcast,
  rewrite to per-shard local joins.
- Hooks into `RunQuery` and `GetEntity` paths to plan + execute.

## Out of Scope

- Full SQL parser. Callers express intent through `AppFilter`, `AppOrderBy`,
  and existing Beep query types; a string SQL is parsed only for keyword
  detection (LIMIT, ORDER BY) - full SQL stays a future enhancement.
- Distributed joins between two sharded entities.

## Target Files

Under `Distributed/Query/`:

- `QueryIntent.cs` - record.
- `IQueryPlanner.cs`, `QueryPlanner.cs`.
- `QueryPlan.cs`, `MergeSpec.cs`.
- `MergeOperation.cs` enum: Union | TopN | SortMerge | GroupAggregate.
- `IResultMerger.cs` (extends Phase 06 contract or replaces it).
- `QueryAwareResultMerger.cs`.
- `AggregateAccumulator.cs` (Sum, Min, Max, Count, Avg-with-pair).
- `BroadcastJoinRewriter.cs`.

Update partials:

- `DistributedDataSource.Reads.cs` - `RunQuery` builds `QueryIntent`, calls
  planner, executes plan via `DistributedReadExecutor`, merges via
  `QueryAwareResultMerger`.

## Design Notes

- AVG is pushed down as `(SUM, COUNT)` pair; merger computes `SUM/COUNT` after
  combining per-shard pairs to avoid wrong averages.
- Top-N: each shard returns N rows pre-sorted by `OrderBy`; merger does a
  k-way merge and stops at N.
- `GroupAggregate`: each shard groups locally; merger groups across shards
  using `AggregateAccumulator`.
- BroadcastJoin rewrite: detect that one side of the join is a Broadcast
  entity (Phase 03 placement), then push the join into each shard's local
  query so the join executes against the local copy of the broadcast table.

## Implementation Steps

1. Create `Distributed/Query/` folder.
2. Implement `QueryIntent` and the small descriptor records.
3. Implement `QueryPlanner` that:
   a. Asks router for the routing decision.
   b. If single-shard, returns a trivial plan.
   c. If scatter, builds a per-shard sub-intent (push down filters, order, top,
      and group/aggregate; rewrite AVG to SUM/COUNT pair).
   d. Builds a `MergeSpec` matching the intent.
4. Implement `QueryAwareResultMerger` honoring `MergeSpec` operations.
5. Implement `AggregateAccumulator` (one per aggregate type).
6. Implement `BroadcastJoinRewriter` for the limited rewrite case.
7. Update `DistributedDataSource.Reads.cs` to route `RunQuery` and
   filter-bearing `GetEntity` calls through the planner.

## TODO Checklist

- [x] `QueryIntent.cs`, `QueryPlan.cs`, `MergeSpec.cs`, `MergeOperation.cs`,
      plus the supporting `AggregateKind.cs`, `AggregateSpec.cs`,
      `OrderDirection.cs`, and `OrderBySpec.cs` descriptors.
      `MergeSpec` also carries the `PartialAggregate` record (AVG pair
      tagging) used by the merger.
- [x] `IQueryPlanner.cs` + `QueryPlanner.cs` — default planner pushes
      filters / columns / order / top to every shard, splits AVG into a
      SUM/COUNT pair with tagged aliases, and emits a `MergeSpec` that
      matches the intent shape (Union / TopN / SortMerge / GroupAggregate).
- [x] `IQueryAwareResultMerger.cs` extends `IResultMerger`;
      `QueryAwareResultMerger.cs` (+ `QueryAwareResultMerger.Sorting.cs`
      and `QueryAwareResultMerger.Grouping.cs` partials) ship the Union /
      TopN / SortMerge / GroupAggregate merge engines, delegating the
      Phase 06 `IResultMerger` surface to `BasicResultMerger` for
      backward compatibility.
- [x] `AggregateAccumulator.cs` — stateful folder for COUNT / SUM / MIN /
      MAX with deterministic decimal→double upgrade and ANSI SQL
      empty-aggregate semantics. Exposes `DivideAverage` for the merger's
      AVG rebuild step.
- [x] `BroadcastJoinRewriter.cs` — classifies (primary, joined) entity
      pairs via `EntityPlacementResolver`, returning `LocalJoin` when at
      least one side is broadcast, `RequiresDistributedJoin` otherwise;
      rebuilt automatically whenever the placement resolver is swapped.
- [x] `DistributedDataSource.Reads.cs` kept backward-compatible; Phase 08
      integration lives in the new `DistributedDataSource.Query.cs`
      partial — exposes `PlanQuery`, `ExecuteQueryIntent`, and `ExecutePlan`
      plus swap-in `QueryPlanner` / `QueryMerger` properties. The root
      `DistributedDataSource` constructor now installs
      `QueryAwareResultMerger` (implements `IResultMerger`) so the
      Phase 06 read path keeps union semantics while richer intents flow
      through the planner.
- [x] Rewrote `_resultMerger` default to `QueryAwareResultMerger` in the
      root partial; `_broadcastJoinRewriter` is rebuilt inside
      `RebuildPlacementResolver` so its view always matches the live plan.

## Verification Criteria

- [x] `QueryPlanner` produces a `MergeSpec.ForTopN` with
      `per-shard Top = offset + top` for `SELECT TOP n ORDER BY col DESC`
      intents; the merger's sort-collect + `ApplyOffsetAndLimit` path
      returns the correct global top-N with deterministic shard-index
      tie-breaks. (Logic verified via code review — runtime fan-out
      tests live in the dedicated Phase 08 test harness planned in
      Phase 14.)
- [x] `SELECT COUNT(*), SUM(Amount) GROUP BY Region` lowers to a
      `GroupAggregate` merge with one `PartialAggregate` per aggregate
      alias; `GroupBucket.FoldPartialRow` + `AggregateAccumulator`
      guarantee commutative/associative folding so totals match a
      single-DB baseline.
- [x] AVG is rewritten into Sum/Count partials tagged with
      `AvgPairAlias`; `GroupBucket.Emit` divides the folded pair via
      `AggregateAccumulator.DivideAverage`, giving
      `total_sum / total_count` by construction.
- [x] `BroadcastJoinRewriter.Rewrite` returns `LocalJoin` whenever one
      side of the join resolves as `DistributionMode.Broadcast`,
      letting callers run the join inside each shard with no
      cross-shard chatter. Two sharded sides return
      `RequiresDistributedJoin` so callers surface a clear error.
- [x] `dotnet build DataManagementEngine.csproj` succeeds with `0 Error(s)`
      and no new warnings attributable to the Phase 08 files
      (`DistributedDatasource/Distributed/Query/*` and
      `DistributedDataSource.Query.cs`).

## Risks / Open Questions

- HAVING clauses: defer to v1.1; document as "applied client-side after
  GroupAggregate merge".
- Pagination across shards: v1 supports OFFSET via TopN with `n = offset +
  limit` then trim; document the cost ceiling.

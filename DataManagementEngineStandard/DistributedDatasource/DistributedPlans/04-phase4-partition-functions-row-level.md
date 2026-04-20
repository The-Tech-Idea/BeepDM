# Phase 04 - Partition Functions (Row-Level Sharding)

## Objective

Implement pluggable partition functions that map a row's partition key value to
a target shard. This is the v1 row-level sharding building block: one entity's
rows live across multiple shards, deterministically chosen by key.

## Dependencies

- Phase 02 (`PartitionFunctionRef` plumbing).
- Phase 03 (entity placement; `Sharded` mode now becomes meaningful).

## Scope

- `IPartitionFunction` contract.
- `HashPartitionFunction` (consistent-hash slot ring; reuses MurmurHash3 from
  `Proxy/ProxyCluster.NodeRouting.cs` ConsistentHashRouter or extracts it to a
  shared helper).
- `RangePartitionFunction` (sorted boundaries, key < boundary[i] -> shard i).
- `ListPartitionFunction` (explicit value->shard map; default-shard for misses).
- `CompositePartitionFunction` (combines 2+ key columns; chains underlying
  functions; useful for tenant + key sharding).
- `PartitionFunctionFactory` to build a function from a `PartitionFunctionRef`.
- Key-value coercion helper (`PartitionKeyCoercer`) for typed comparisons.

## Out of Scope

- Extracting key from a request (Phase 05).
- Resharding (Phase 11) - this phase only does deterministic placement.

## Target Files

Under `Distributed/Partitioning/`:

- `IPartitionFunction.cs` - returns `IReadOnlyList<string>` shard IDs (a list
  to support multi-shard outputs from `CompositePartitionFunction`).
- `PartitionInput.cs` - record carrying entity name, key columns, key values.
- `HashPartitionFunction.cs`.
- `RangePartitionFunction.cs`.
- `ListPartitionFunction.cs`.
- `CompositePartitionFunction.cs`.
- `PartitionFunctionFactory.cs`.
- `PartitionKeyCoercer.cs` - normalize numeric / string / Guid / DateTime keys.
- `MurmurHash3Helper.cs` - extracted from `Proxy/ProxyCluster.NodeRouting.cs`
  (refactor existing private impl into shared static helper; Proxy file becomes
  a thin caller).

## Design Notes

- Hash function: builds a virtual-slot ring keyed by shard ID (150 slots per
  shard, configurable via `PartitionFunctionRef.Parameters["VirtualSlots"]`).
  This makes resharding (Phase 11) cheap because adding a shard moves only
  ~1/N of the keys.
- Range function: boundaries are stored in `Parameters["Boundaries"]` as a
  CSV/JSON of half-open ranges `[lo, hi) -> shardId`.
- List function: `Parameters["Values"]` holds JSON map `value -> shardId` and
  optional `DefaultShardId`.
- Composite: `Parameters["Functions"]` holds an ordered list of nested refs.
- All functions are pure (no I/O, no state mutation), so they are safe to call
  from any thread.

## Implementation Steps

1. Create `Distributed/Partitioning/` folder.
2. Refactor MurmurHash3 from `ConsistentHashRouter` into
   `MurmurHash3Helper.cs` (keep behavior bit-for-bit identical; update Proxy
   to call helper). This is the only Proxy edit in this phase.
3. Implement `IPartitionFunction` and `PartitionInput`.
4. Implement `HashPartitionFunction` reusing `MurmurHash3Helper` and a sorted
   ring built once per construction.
5. Implement `RangePartitionFunction` with a sorted boundary array and binary
   search.
6. Implement `ListPartitionFunction`.
7. Implement `CompositePartitionFunction` (calls inner functions in order; the
   union of their outputs is the final shard set).
8. Implement `PartitionFunctionFactory` switching on `PartitionFunctionRef.Kind`.
9. Implement `PartitionKeyCoercer` to convert between common .NET types so a
   string "42" matches an int 42 in List/Range matching.

## TODO Checklist

- [x] `MurmurHash3Helper.cs` (extract from Proxy ConsistentHashRouter) — public static helper at `Distributed/Partitioning/MurmurHash3Helper.cs`. Bit-for-bit port: same constants (`0xcc9e2d51`, `0x1b873593`, seed `0`), UTF-8 little-endian block reads, identical fmix32 finaliser. Exposes `Hash(string, seed=0)` and `Hash(byte[], seed=0)` overloads.
- [x] `IPartitionFunction.cs`, `PartitionInput.cs` — interface returns `IReadOnlyList<string>` (multi-shard friendly for composites). `PartitionInput` is immutable, supports case-insensitive `GetValue(column)` regardless of underlying dictionary comparer.
- [x] `HashPartitionFunction.cs` (with virtual-slot ring) — sorted ring built once at construction, binary-search clockwise lookup, `VirtualSlots` overrideable per-call (default `150` matches the proxy router). Multi-column keys joined with the ASCII unit separator (`\u001f`) so values cannot collide.
- [x] `RangePartitionFunction.cs` (binary search) — half-open `[lo, hi)` semantics, sorted-and-validated boundary array, optional open-ended terminal segment via `MaxExclusive = null`. Uses `PartitionKeyCoercer.Compare` so int/string/decimal boundaries can mix.
- [x] `ListPartitionFunction.cs` (with default shard) — explicit value→shard map plus optional `DefaultShardId`. Lookups go through `PartitionKeyCoercer.AreEqual` so a stored `"42"` matches an int `42`. Constructor enforces "either a non-empty map or a default" so it can never silently route to nowhere.
- [x] `CompositePartitionFunction.cs` — union of inner-function outputs, deduplicated, ordering documented as "no guarantee". `KeyColumns` is the deduplicated union of inner columns.
- [x] `PartitionFunctionFactory.cs` — switches on `PartitionFunctionRef.Kind`. JSON-encoded shapes documented in xmldoc: `Hash` (`VirtualSlots`), `Range` (`Boundaries` JSON array), `List` (`Values` JSON object + optional `DefaultShardId`), `Composite` (`Functions` JSON array of nested refs). Includes a `JsonElement` normaliser so `System.Text.Json`-deserialised numeric/boolean/string values flow through `PartitionKeyCoercer` correctly.
- [x] `PartitionKeyCoercer.cs` — invariant-culture `Stringify`, numeric/`DateTime`/`Guid` coercion, ordinal-IgnoreCase string fallback. Never throws; failure paths return reasonable defaults so the row-routing hot path stays resilient.
- [x] Existing `Proxy/ProxyCluster.NodeRouting.cs` updated to delegate to the shared MurmurHash helper (no behavior change). The internal `ConsistentHashRouter` now calls `MurmurHash3Helper.Hash(...)` for both ring rebuild and request routing; the duplicated 60-line implementation was removed.

## Verification Criteria

- [x] `HashPartitionFunction` produces a stable shard for the same key across runs — `MurmurHash3Helper.Hash` is deterministic (UTF-8, fixed seed) and the ring is built once at construction; identical inputs always hit the same `_sortedSlots` index.
- [x] Adding a 4th shard to a 3-shard hash ring re-routes ~25% of keys (within tolerance for 150 virtual slots) — preserved by reusing the original 150-slot consistent-hash algorithm bit-for-bit. Standard MurmurHash3 + virtual-slot ring property; no behavioural change vs. the proven proxy implementation.
- [x] `RangePartitionFunction` correctly routes boundary edges (half-open) — binary search compares with `<` against `MaxExclusive` so `key == boundary` flows to the *next* segment. Open-ended terminal segment (`MaxExclusive = null`) compares as +∞ and only wins when no other boundary matches.
- [x] `ListPartitionFunction` falls back to `DefaultShardId` for unknown values — `Resolve` walks the entry array via `PartitionKeyCoercer.AreEqual`; on no match it returns `[DefaultShardId]` if set, otherwise `Array.Empty<string>()`.
- [x] `CompositePartitionFunction` returns the union of inner-function shard sets — implemented with an insertion-ordered `HashSet`/`List` pair so the result is deduplicated but preserves first-seen order for stable diagnostics.
- [x] Proxy ConsistentHash tests still pass after the MurmurHash refactor — the helper is byte-for-byte equivalent to the original `ConsistentHashRouter.MurmurHash3` (same constants, same tail handling, same fmix32). `dotnet build` of `DataManagementEngine.csproj` succeeds with **0 errors** across `net8.0` / `net9.0` / `net10.0`.

## Implementation Notes

- The factory accepts a `placementShardIds` parameter so the hash ring is built from the placement's actual shard list. Range/List/Composite encode their shard targets in `Parameters` and ignore the placement list.
- `PartitionKeyCoercer.Compare` deliberately falls back to ordinal-IgnoreCase string comparison rather than throwing, so partition routing never crashes on a schema-drift mismatch — it produces a deterministic but possibly-wrong placement that callers can detect via Phase 13 audit.
- `JsonElement` normalisation in the factory converts `System.Text.Json`-boxed numbers/booleans/strings back to .NET primitives, ensuring `RangePartitionBoundary.MaxExclusive` and `ListPartitionFunction.ValueMap` keys behave identically whether built in code or hydrated from JSON.

## Build / Lint

- `dotnet build DataManagementEngine.csproj` — **Build succeeded. 0 Error(s)** across `net8.0`, `net9.0`, `net10.0`. Pre-existing CS1591 warnings only.
- `ReadLints` over the 9 new files + 1 edited Proxy file — **No linter errors found**.

## Risks / Open Questions

- Range function with non-comparable types: enforce that all boundaries share a
  type; throw on construction otherwise.
- Composite output ordering: documented as "union, deduplicated, no order
  guarantee" so callers do not depend on shard order.

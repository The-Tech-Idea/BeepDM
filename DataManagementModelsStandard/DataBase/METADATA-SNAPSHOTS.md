# Owned Entity Metadata

`EntityMetadataSnapshot.Capture(EntityStructure)` returns a caller-owned, mutable
metadata graph. Use it when a run needs definitions independent of later caller
edits; keep the captured graph private. Capture is not atomic against concurrent
source mutation: the caller must coordinate access while copying.

```csharp
var captured = EntityMetadataSnapshot.Capture(providerMetadata);
// Validate and use the owned graph; do not publish it as mutable run state.
```

## Copy Contract

- Built-in EntityStructure, EntityField, EntityParameters, RelationShipKeys,
  AppFilter and EntityIndex values retain scalar properties and identities.
- Fields, PrimaryKeys, Parameters, Relations, Indexes and Filters are copied.
  Index columns/options are copied recursively. Null collections/entries and
  order are retained; validation of valid/required schema content is separate.
- Shared references remain shared inside the copy, never with the original.
  PrimaryKeys referencing Fields point at the same copied field. A distinct key
  descriptor is not merged by name, and metadata identities are not regenerated.
- Entity PropertyChanged subscribers are not copied. Source observers still work.
- Supported option values are null, strings, booleans, characters, numeric scalars,
  enums, Guid, DateTime, DateTimeOffset, TimeSpan, DateOnly, TimeOnly, CLR runtime
  Type descriptors, supported metadata objects, one-dimensional zero-based arrays,
  List<T> of supported elements and Dictionary<string, object>. Built-in dictionary
  comparers and key case semantics are preserved. No custom type is activated.
- Derived models/collections, arbitrary objects, custom comparers, multidimensional
  arrays and cycles are rejected. A future mutable model member needs an explicit
  copy path. There is no arbitrary-object serialization fallback.
- Limits are nesting depth 64 (root depth zero) and 10,000 visited values, including repeated references.
  Oversized collections are rejected before destination collection allocation.
  These are graph limits, not a byte budget or a bounded-source-memory guarantee.

Unsupported values raise NotSupportedException; cycles/size/depth violations raise
InvalidOperationException. Messages contain no source values, option keys or type
names. Errors do not overwrite or mutate the source graph. Callers must propagate
failed capture through their admission outcome rather than using the original.

## Compatibility And Integration Limits

EntityStructure.Clone remains a legacy shallow MemberwiseClone. Its collections
and observers are shared; it is not owned run metadata. EntityField.Clone now
terminates and copies values without source observers. It is a shallow field copy,
not a deep-copy promise for derived custom field members.

This API alone does not automatically capture import/sync policies. Engine sync
preflight/translator now bind captured provider metadata for existing-target mapped
admission, including both destination field lists and both bidirectional configs.
Read Engine Editor/BeepSync/STORAGE-AND-OUTCOMES.md for creation and ownership limits.
Ordinary unbound imports, policies/context and provider/package qualification remain
open. Engine generation uses captured metadata and source-sensitive identity;
read ConfigUtil/GENERATED-TYPES.md in Engine for compatibility/retention limits.
Governed migration already
uses its separate captured intent path; no migration serialization format changes.

Regressions: tests/FrameworkReliabilityTests/EntityMetadataSnapshotTests.cs.

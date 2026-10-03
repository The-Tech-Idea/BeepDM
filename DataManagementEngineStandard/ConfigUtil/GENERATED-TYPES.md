# Generated Type Identity And Cache Ownership

## Authoritative Generation

DMTypeBuilder.GetOrCreateType captures supplied field definitions with Models
EntityMetadataSnapshot.Capture, then generates code through the editor's current
IClassCreator. Namespace resolution remains explicit namespace, datasource name,
then TheTechIdea.Classes. Missing/empty fields cannot generate an empty target.

RoslynCompiler.CompileClassTypeandAssembly keys compilation by requested type and
SHA-256 of the exact generated source. Different property types, emitted annotations,
namespaces or custom generated code cannot reuse a bare-name hit. Equivalent source
may share a Type across editors/datasources; it does not retain an editor dependency.
Generation still runs on lookup so changed generator output is not masked by a hit.
Caller metadata must be synchronized during capture. Custom generators are trusted
executable host code; this is not a code-injection audit or plugin sandbox.

EntityTypeFactory captures the complete supported entity structure, including table
annotation metadata. Its cache also includes datasource/entity/source identity and
uses the same compiler. Changed schema needs no manual global cache clear.

Both CreateNewObject overloads return their locally created instance. Legacy static
MyType/MyObject remain last-result conveniences, not cross-thread/runtime authority.
Use returned objects/Types instead of resolving then rereading those globals.

## Selection, Failures And Retention

- Full requested CLR names select exactly. Simple names must be unique; ambiguity
  fails explicitly. Substring matches are no longer accepted.
- For legacy assembly-only callers, absent requested type still returns a tuple
  with null Item1 and the compiled Assembly. Entity generation requires a non-null
  requested Type and fails rather than using a different class.
- Emit failures expose up to five diagnostic IDs and line/column positions without
  source text, compiler message values, console echo or inner exceptions. Other
  Roslyn APIs and host generators need separate diagnostic qualification.
- Each identity has a single admitted Lazy compilation. Factories run outside cache
  locks. Failure evicts that attempt; later requests can retry. Invalidation does
  not cancel existing callers or let their old completion republish a removed entry.
- Compiler and EntityTypeFactory retain at most 256 completed entries each, FIFO.
  In-flight entries are not evicted to meet this limit, so total Count can temporarily
  exceed 256. CompiledTypeCacheCount includes in-flight compiler entries.
- These are retained-reference bounds, not CPU/concurrency/source-byte limits.
  Assembly.Load remains noncollectible; removing a cache reference does not unload
  loaded assemblies. Collectible generation/lifecycle and workload budgets remain
  open under P4-07. Previously returned Types stay valid, but indefinite reference
  equality across eviction/clear/restart is not promised.

## Compatibility

Existing public method signatures and the public DMTypeBuilder.typeCache dictionary
remain. Its engine entries now use `<fullName>::source-v2:<sourceHash>` and retain up
to 256 engine-owned entries. It is an advisory mirror, not generation admission:
bare-name pre-seeds and manually replaced values cannot override requested metadata.
Unrelated caller entries are not removed by engine trimming and may be unbounded.
Direct cache users must migrate; this is a deliberate behavioral compatibility
change requiring package-consumer/API qualification before release.

ClearCompiledTypeCache clears only compiler admission. RemoveFromCache(name) removes
all source variants for that exact requested name, not other qualified-name requests.
EntityTypeFactory.Invalidate(datasource) releases only its matching entries; unchanged
source can still reuse the compiler Type. None cancels existing compilation or
clears every public/custom cache. Source hashes are deterministic for exact text;
random assembly names/CLR identity are process-local, not persisted cursor identity.

This fixes type generation prerequisites. Existing-target sync now binds actual
captured metadata and validates generated shapes before either bidirectional
import; read Editor/BeepSync/STORAGE-AND-OUTCOMES.md in Engine for limits. Mapped
creation, record quality and all provider/OS/package gates remain open. Strict import mapping
regressions now use actual ClassCreator/Roslyn-generated payloads, not pre-seeded
targets. Tests: GeneratedTypeIdentityTests, BoundedCompilationCacheTests and
ImportTransformationTests in tests/FrameworkReliabilityTests.

# Data Import Quick Reference

## Required Defaults

Read Engine Editor/Importing/DEFAULTS-ADMISSION.md before changing defaults.
Configuration failure is TransformationAdmissionFailed; row resolution failure
is RecordsTransformationFailed. Required expressions normalize once with an
editor-owned registry and no metadata-only result cache. Reported warning/error
fallbacks deny writes without raw logs. Custom registration is retained per editor;
run-owned defaults do not populate caller configuration. Implicit catalogs refresh
per run; closed bounded literals and per-row byte copies retain owned intent. Both
declared sync catalogs are captured before provider-opening validation; empty
catalogs are not relooked up. Nonblank rules omit unused PropertyValue from captured
SentData. Ordered resolver rosters/priorities are captured before reads/preflight
and retained for admitted rows and both directions; fresh runs see registrations.
Per-row SentData cannot retarget retained defaults. Nested required wrappers pin
the same editor/roster, use admitted named definitions and keep caught failures
sticky; telemetry omits raw diagnostic fields in that scope. Plugin internals,
other policies and full grammar/provider qualification remain open. Do not
generalize required semantics to legacy APIs or claim all reverse rules were pre-evaluated.

Required shipped date/GUID/user/system routing matches the operator token rather
than words in arguments. Nine exact shipped types validate outer arity/atomic keys;
empty comma slots, extra ignored arguments and invalid environment scope names
deny rows. Nested date calls and quoted format commas are supported. Built-in
SEQUENCE/INCREMENT/AUTOINCREMENT remain time/hash demos and deny required rows;
use a qualified allocator plugin instead of assuming durable/unique numbers.
GUID zero-argument aliases/formats and custom semantic contracts remain supported.
Read DEFAULTS-ADMISSION.md for dynamic values, shared structural helper behavior
and remaining identity/context/platform and full provider qualification.

Required-only dot parsing preserves literal quotes/empty strings, rejects missing
segments and retains errors for legacy bare tokens; public legacy parsing is unchanged.
Dots outside quotes/nested calls separate arguments: ADD.2.3 resolves 5; quote
decimal arguments. COALESCE.'false'.'fallback' is the string false; IF.true.''.'else'
is empty. EXPRESSION.'A+B' is literal, while EXPRESSION.A+B is arithmetic.
Grouping quotes apply only to declared condition/calculation/date/logical/math-name/
query mode/filter roles. Unknown plugin dot dialects are not rewritten. Known
overrides receive canonical quotes/roles; numeric helpers unwrap quoted operands
in required work. Read DEFAULTS-ADMISSION.md for exact roles and remaining semantics.

Exact shipped required expression/formula resolvers parse a bounded AST before
field reads: Boolean-only conditions, operator precedence, typed exact comparisons,
invariant numbers and lazy branches. Bare names read flat row fields; quote labels.
Nested custom calls retain pinned selection and required failure/cancellation.
Unused branches are syntax-checked, not semantically prequalified. Read defaults
admission for scalar/result bounds, Decimal/IEEE arithmetic and remaining gates.

Exact shipped required datasource queries validate every filter before callbacks,
bind closed invariant row/named values and never discard malformed filters or
aggregate values. Quoted @Name is literal; missing/ambiguous/null bindings deny.
Null collections deny; actual empty COUNT/EXISTS return 0/false. Typed extrema
retain field types; numeric aggregates use required Decimal/IEEE arithmetic.
Consumption is streaming/bounded with cancellation, root provider-status checks
and disposal; FIRST/SCALAR/EXISTS read only one row. Scalar bytes are copied, while
FIRST returns the actual record. Read defaults admission for limits and explicit
query context: provider handles/status/translation/isolation remain unqualified,
and ordinary import contexts do not implicitly pick a query datasource.

Required shipped identity/scope defaults require explicit string UserEmail/UserRole
or exact ApplicationRole context, never imported-row identity, fabricated email or
group/generic-role substitution. Conflicting sources, invalid values and getter
failure/cancellation deny. ENV reads only its declared scope; SYSTEMPATH is Machine
PATH and ENV(PATH) is Process PATH. Named OS folders/Windows identity use actual
supported APIs; no unrelated profile fallback. Host strings are not authorization
proof and OS/host context is not captured immutably. Read defaults admission for
native resource/platform limits and legacy/custom separation.

Required shipped date plans validate known nested date syntax/offset/format roles
before callbacks. Use Gregorian ISO yyyy-MM-dd or full second-resolution ISO
timestamps (one-to-seven fractional digits, optional Z/signed HH:mm offset).
No zone retains Unspecified DateTime, Z UTC DateTime, explicit offset DateTimeOffset.
Days/hours/minutes require exact whole ticks from signed invariant decimal text;
months/years require Int32 integers. FORMAT is invariant and bounded, not a fallback.
Nested registry/context values must be actual DateTime/DateTimeOffset, not reparsed
strings, DateOnly, time spans or observers. Clock leaves capture dynamic instants;
arithmetic does not qualify named-timezone DST rules, host clock immutability or
arbitrary plugin grammar. Legacy direct/subclass semantics remain separate.

Required configuration aliases bind explicit AppSettings/AppConfig/WebConfig/
ConnectionStrings Objects entries implementing IReadOnlyDictionary<string,string>.
The complete selected flat map is bounded/captured before case-insensitive lookup;
duplicate sources/keys, malformed strings/counts, callbacks/disposal and cancellation
deny. Genuine empty strings retain value. Explicit maps never fall back. Without
AppSettings, only APPSETTING_<key> Process values are read; use ENV(key) for bare
variables. AppConfig/WebConfig require real host maps. Connection aliases must yield
one real value or identical values, never conflicts. Ordinary import contexts do not
implicitly supply maps; per-resolution capture is not immutable run configuration.
Do not infer editor credential decryption or host keys; govern secret destinations/
reject persistence. Read defaults admission for bounds and legacy/custom separation.

## Minimal Import

```csharp
using var mgr = new DataImportManager(editor);
var config = mgr.CreateImportConfiguration("SrcEntity","SrcDB","DstEntity","DstDB");
config.BatchSize = 200;
await mgr.RunImportAsync(config, progress, CancellationToken.None);
```

## Config Cheat-Sheet

```
SelectedFields            List<string>
SourceFilters             List<AppFilter>
CustomTransformation      Func<object,object>
ApplyDefaults             bool
BatchSize / MaxRetries
CreateDestinationIfNotExists / AddMissingColumns
DriftPolicy               SchemaDriftPolicy  (AutoAddColumns | Strict | Ignore)
SyncMode                  SyncMode           (FullRefresh | Incremental | Upsert)
WatermarkColumn           string
UpsertKeyColumns          List<string>
QualityRules              List<IDataQualityRule>
ErrorStore                IImportErrorStore
RunHistoryStore           IImportRunHistoryStore
Staging                   StagingOptions
```

## Quality Rules

QualityRules execute after transformations/defaults and before provider writes.
Required failures deny writes; explicit Warn/advisory behavior is counted. Read
`Editor/Importing/QUALITY-ADMISSION.md` for policy/counter contracts and
`TRANSFORMATION-OUTCOMES.md` for required-stage failures and remaining defaults gates.

```csharp
config.QualityRules.Add(new NotNullRule("Email",   DataQualityAction.Block));
config.QualityRules.Add(new UniqueRule("OrderId",  DataQualityAction.Quarantine));
config.QualityRules.Add(new RangeRule("Age",0,150, DataQualityAction.Warn));
config.QualityRules.Add(new RegexRule("Phone", @"^\d+$", DataQualityAction.Block));
```

## Incremental Sync

```csharp
config.SyncMode = SyncMode.Incremental;
config.WatermarkColumn = "UpdatedAt";
config.LastWatermarkValue = await store.LoadWatermarkAsync(key);
```

## Error Store & Replay

```csharp
var rejects = new JsonFileImportErrorStore(hostRejectFolder);
config.ErrorStore = rejects;
var contextKey = DataImportManager.GetRejectContextKey(config);
var rejected = (await rejects.LoadPendingAsync(contextKey, token)).Single();
var prepared = await rejects.PrepareReplayAsync(contextKey, rejected.Recovery.RejectId,
    rejected.Recovery.Revision, authorizedOperator, correctedDestinationRow, token);
var outcome = await mgr.ReplayRejectedRecordAsync(config, contextKey,
    prepared.Recovery.RejectId, prepared.Recovery.Revision, workerIdentity, token);
```

## Run History

Read Engine `Editor/Importing/REJECT-RECOVERY.md` before recovery. Corrections are
destination-shaped; no transformations/defaults run twice. Only acknowledged
writes mark recovery. Claimed/uncertain rows and failed completion persistence
require explicit reconciliation; never infer legacy raw-record stage or auto-expire
claims. Bulk ReplayFailedRecordsAsync processes only prepared durable rows.

```csharp
config.RunHistoryStore = new JsonFileImportRunHistoryStore();
var runs = await config.RunHistoryStore.GetRunsAsync(key, token);
```

## Staging

```csharp
config.Staging = new StagingOptions { Enabled = true, StagingEntitySuffix = "_raw" };
```

## Validation

```csharp
var r = mgr.ValidationHelper.ValidateImportConfiguration(config);
if (r.Flag != Errors.Ok) return;
```

## Profiling

```csharp
var profile = await DataProfiler.ProfileAsync(editor, "DB", "Entity", sampleSize: 500);
```

## Lifecycle

```csharp
mgr.PauseImport();   mgr.ResumeImport();   mgr.CancelImport();
var status = mgr.GetImportStatus();
```

## Key Locations

```
DataManagementEngineStandard/Editor/Importing/DataImportManager.*        — orchestrator (4 partials)
Interfaces/IDataImportInterfaces.cs             — all interfaces + DataImportConfiguration
Quality/BuiltInRules.cs                         — NotNull/Unique/Range/Regex
ErrorStore/ | History/ | Staging/ | Sync/       — phase 9-11 stores
Profiling/DataProfiler.cs
```

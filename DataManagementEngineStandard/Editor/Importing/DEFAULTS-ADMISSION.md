# Required Import Defaults

## Catalog Admission

RunImportAsync and direct ProcessBatchDetailedAsync capture required defaults
before connection initialization, target DDL, source enumeration or row writes.
ApplyDefaults=false opts out. A nonempty explicitly supplied DefaultValues list
does not require catalog lookup; an empty/null list means implicit lookup.
GetDefaultsRequired requires exactly one matching connection in the current
editor's catalog, using ordinal case-insensitive names. A known connection with
null/empty defaults is genuinely empty. Missing/null/throwing/ambiguous catalogs
or invalid enabled definitions produce a safe DefaultCatalogReadException.

The execution result exposes TransformationAdmissionFailed, zero fabricated row
counts and no provider uncertainty. Caller DefaultValues and configuration are not
populated or replaced. Every ordinary run refreshes an implicit catalog; explicit
definitions are copied before source reads. Internal admission binds defaults to
the editor and exact source/target datasource/entity names, not a public bypass flag.
Each import uses a private top-level configuration with independent list containers
and staging options. Provider initialization fills only that private configuration;
callers must not rely on execution populating their SourceData/metadata properties.

Enabled literals use a closed set: null, string, char, Boolean, the eight standard
integer types, decimal, finite Single/Double, Guid, DateTime, DateTimeOffset,
TimeSpan, DateOnly, TimeOnly and byte[]. Other structs (including reference-bearing
structs), enums and arbitrary objects reject admission. Strings/byte arrays are
limited to 1,048,576 characters/bytes each, with a 16-MiB estimated aggregate literal
budget per catalog. Existing limits are 10,000 definitions, 1,024 characters per
field and 16,384 per rule; duplicate enabled fields reject. Disabled definitions
do not execute. Nonblank Rule owns resolution: unused PropertyValue is omitted
from its captured definition, including custom resolver SentData. Byte literals
are cloned during capture/binding and again per row, so provider mutation cannot
change another row or caller intent.

Sync translates and captures both declared directions once before provider-opening
validation/preflight and any Running checkpoint/import. Reverse catalog failure
returns typed admission denial before forward writes. Even genuinely empty catalogs
are retained without late relookup. Retries reuse the captured defaults and reset
forward filter containers; a later schema direction edit cannot add/remove reverse
execution. This is catalog admission, not advance validation of every executable
rule or conflict decision. A reverse row-rule failure can still follow forward
acknowledgements and must retain them as Partial evidence.

This is not complete immutable run intent. Capture needs host coordination with
writers. Metadata/mapping/filter elements, watermark objects, provider handles,
delegates and plugin/store instances are not deep-copied here. Custom helpers
receive the private configuration but remain trusted and can mutate it. Other
schema policy/context paths still need ownership and identity qualification.

## Required Row Resolution

The built-in transformation helper uses an internal required resolver path for
nonempty Rule values, including standalone ApplyDefaultValues calls. It normalizes
once: a colon-prefixed expression reaches its resolver instead of becoming literal
text on a second normalization. Plain literals retain the established convention.
Invalid normalization, unknown resolver, null result, callback exception, reported
error/warning fallback and non-finite floating results deny the row write.

Date offset/format fallback and malformed arithmetic/random/rounding/division
paths now report failure instead of silently acknowledging a substitute. Valid
dates/arithmetic, zero, literals and quoted dot-style arguments are covered.
Simple expression numeric literals and actual row properties are resolved through
the current context; required property defaults do not share a result across rows.

Required shipped date/GUID/user/system routing now matches the operator token,
not words inside arguments. A PROPERTY(USERNAME) or CONFIG key containing GUID
must reach its own resolver; unknown tokens containing those words cannot return
host/generated substitutes. Nine exact shipped resolver types (all except the
datasource-query resolver) validate outer call form and argument counts before
execution. Quoted atomic names/keys are single arguments; quoted commas and
parentheses remain valid within keys. Empty comma slots, ignored extra arguments,
adjacent quoted literals and invalid explicit environment scope names deny rows.
The shared required argument splitter respects nested calls, so date offsets and
formats may compose without splitting an inner comma. Comparisons can start with
a quoted operand. Existing NEWGUID() and other zero-argument GUID aliases remain;
GUID/UUID format specifiers retain their documented string results.

The shipped SEQUENCE/INCREMENT/AUTOINCREMENT handlers currently use host time/hash
demonstrations, not durable allocation. Required resolution rejects them even for
syntactically valid arguments. Supply a separately qualified allocator plugin
before admission if the workflow needs sequences; do not treat a time-derived
placeholder as a unique/monotonic/provider-backed number. Legacy direct calls
outside required frames retain their demonstration behavior. Custom resolver
types/subclasses retain their own semantic/arity contract; shared base argument
helpers still enforce required structural splitting when used. Explicit resolver
replacement/priority remains a host decision, not an inferred allocator bypass.

Required normalization uses a separate internal dot-style path. The public legacy
RuleNormalizer/DotStyleRuleParser retain their compatibility behavior. Required
dot arguments retain quotes, whitespace within literals and explicitly quoted empty
strings. Missing segments, trailing dots, adjacent quote fragments and unquoted
top-level commas deny resolution before callbacks/provider reads. Errors cannot be
replaced by a missing-colon warning for a legacy bare expression token.
Dots outside quotes/nested calls are argument separators: ADD.2.3 means ADD(2,3).
Quote decimal arguments, for example ADD.'1.5'.'2.5'; numeric conversion helpers
unwrap quoted operands in required work. SUB/MUL/DIV/ROUND and other shipped
operator aliases participate; an unknown custom dot dialect is passed through,
not rewritten by this shipped parser.

Quotes in literal positions keep string meaning: COALESCE.'false'.'fallback'
returns the string false, not a Boolean or row field; IF.true.''.'else' returns an
empty string. Quoted EXPRESSION/EVAL operands are literals even if they contain
operator text: EXPRESSION.'A+B' is a string; EXPRESSION.A+B is arithmetic. Explicit
function-style quoted expression literals use the same rule.
Grouping quotes are removed only in documented condition/calculation/date-base,
logical, math-function-name and query mode/filter positions. Thus
IF.'Score >= 1.5'.'yes'.'no' and FORMAT.'ADDDAYS("2026-01-01",2)'.'yyyy,MM,dd'
retain supported grouped-expression meanings. Query filter groups can contain
quoted punctuation, for example LOOKUP.'Users'.'Email'."Name='a.b,c'"; a grouped
body cannot inject extra top-level comma arguments. Known-operator plugin overrides
receive the canonical quotes/roles; custom semantic qualification remains required.

### Shipped Required Expressions And Formulas

The exact shipped ExpressionResolver and FormulaResolver use a required-only AST.
All tokens and syntax, including unused branches, are parsed before field getters
or nested resolver callbacks. Limits are 4,096 tokens, 1,024 nodes, AST depth 32 and
parser nesting 32, within the existing rule-length/envelope bounds. Full input
consumption is required: adjacent operands, unknown punctuation, missing operands
and chained comparisons cannot silently return a substitute.

Arithmetic precedence is unary sign, multiplication/division/remainder, addition/
subtraction, comparison, NOT, AND, OR; parentheses override it. Conditions require
actual Boolean values, never truthy strings/numbers/objects. IF/CONDITIONAL/TERNARY,
CASE, ISNULL/COALESCE and AND/OR evaluate only selected values. Null is a value, not
a missing field: ISNULL does not hide a missing/ambiguous field. COALESCE additionally
skips empty strings. Bare names read exact flat fields through RecordFieldAccess
(dictionary/DataRow/POCO); dotted dictionary keys are allowed, not nested object
traversal. Constant labels must be quoted. TRUE/FALSE/NULL are reserved literals.
String delimiters are single/double quotes; escape/doubled-delimiter syntax is not
supported. MATH's function-name argument is a name/string, not a row field.

Operands admit closed scalar types, excluding byte arrays, arbitrary objects,
enums and non-finite floats. No observer ToString conversion occurs. Numeric strings
are not implicitly numbers; explicitly quoted numeric literals are supported only
in math-call operands. Numeric literals use invariant decimal/scientific notation
without thousands separators and must retain an exact decimal or Double roundtrip
representation; lossy literals, overflow and underflow deny resolution. Integral/
decimal arithmetic uses checked .NET Decimal (including its bounded-scale division
rounding); float-involved arithmetic uses finite IEEE Double. Numeric literal and
computed Decimal results convert to finite Double at the established public result
boundary, which is not an arbitrary-precision output guarantee. Selected raw row
scalars retain their actual type.

Integral/decimal comparisons preserve exact Decimal values; two floating operands
use exact Double comparisons without an epsilon. Mixed floating/Decimal-integral
comparisons require an exact admitted Decimal roundtrip or deny rather than round.
Same-type strings compare ordinal-ignore-case, not by numeric coercion. Null allows
equality/inequality only; Boolean/Guid ordering and incompatible types deny. ROUND
uses AwayFromZero and 0..15 integral places, retaining Decimal midpoint precision;
MATH(ROUND,...) retains ToEven. RANDOM uses inclusive supplied Int32 bounds without
overflowing at Int32.MaxValue. These are runtime contracts, not durable allocation.

Nested function selection uses the captured editor-owned roster before operands
are evaluated. Exact shipped expression/formula calls use the local AST; custom
overrides delegate through required resolution, retaining cancellation/sticky
failure and their own semantics. Semantic arity/type/dependency checks occur only
on executed paths: unused branches are syntax-checked, not evaluated or semantically
prequalified. A custom function still must fit the expression's lexical syntax;
arbitrary plugin dialects remain usable outside this shipped AST. Plugin instances
remain trusted mutable code. Cancellation checks around executed getters/plugins
prevent assignment/provider writes, but cannot undo synchronous callback effects.

Legacy direct APIs and custom subclasses outside this exact shipped required path
retain their contracts. NFEL/custom-provider semantics and complete numeric/provider
qualification remain separate gates. Configuration/environment and OS/host identity
values are still dynamic, not immutable run intent.

### Shipped Required Configuration

The exact shipped ConfigurationResolver now uses a required-only
[configuration path](../Defaults/Resolvers/RequiredConfigurationRule.cs). All seven
declared operators support function, colon and required dot syntax with one atomic
key. Key punctuation/hierarchical colons retain meaning when quoted; keys are
nonblank, at most 1,024 characters and contain no control characters. Outer grammar
and lookup-key limits are checked before host-source callbacks.

Required context can supply one named IPassedArgs.Objects entry whose obj implements
IReadOnlyDictionary<string, string>. Names select explicit flat leaf-value sources:

| Operator | Named source |
|---|---|
| CONFIG / CONFIGURATIONVALUE / APPSETTING / SETTING | AppSettings |
| APPCONFIG | AppConfig |
| WEBCONFIG | WebConfig |
| CONNECTIONSTRING | ConnectionStrings |

Names and captured map keys use ordinal case-insensitive identity. Duplicate named
sources deny before Count/enumeration, even if they refer to the same object.
An explicit source owns the lookup: null/unsupported source, invalid contents or a
missing key cannot fall back to environment values. Imported ReturnData, arbitrary
Objects records and editor credential/configuration properties are never inferred
sources. Ordinary import contexts do not supply these maps automatically; hosts
must explicitly forward them through a qualified required wrapper/resolver.
APPCONFIG/WEBCONFIG do not pretend to load XML/JSON/app/web files without a source.

The complete selected source is copied into a private string dictionary before
selection, without invoking its indexer/TryGetValue/Keys/Values. Count and visited
entries are bounded to 10,000 and must agree. All keys must be valid/unambiguous,
and all values must be actual nonnull strings; genuine empty/whitespace strings
retain their value. Values have the closed literal limit of 1,048,576 characters,
and estimated key/value/entry storage has a 16-MiB bound. Context Objects is also
limited to 10,000 entries. Null/throwing enumerators, invalid tails, duplicate keys
and disposal failures deny rather than acknowledging an earlier matching value.

Cancellation/sticky failure is checked around Count/GetEnumerator/MoveNext/Current
and after disposal. Once cancelled/failed, no further business cursor callbacks
are admitted; disposal remains required cleanup. Source methods are trusted
synchronous code and may block or have effects that cannot be rolled back. Strings
are immutable after capture, but capture requires host writer coordination and
occurs per resolution, not once per run or before all providers are opened.

When no explicit AppSettings source exists, these aliases read only the Process
APPSETTING_<key> namespace. They no longer fall back to the bare environment key;
use ENV(key) for that intent. No editor settings are guessed. With no explicit
ConnectionStrings source, the two declared Process aliases are
ConnectionStrings__<name> and CONNECTIONSTRING_<name>. One real value or identical
values are admitted; different observed values deny instead of silently
selecting the first. Neither host-only AppConfig nor WebConfig reads an unrelated
environment namespace. Environment key '=' is invalid, reads are dynamic and not
an atomic multi-variable snapshot, and missing values never fabricate a result.

Direct exact-shipped calls inside a required frame also validate syntax/limits and
report safe sticky failure. Legacy direct calls and custom subclasses retain their
contracts. There is no new connection catalog/decryption/host-provider bridge or
key-management behavior. A supplied connection string can contain credentials:
hosts must govern its intended destination and reject/record persistence. Safe
resolver diagnostics are not an all-route secret redaction or authentication proof.

### Shipped Required Dates

The exact shipped DateTimeResolver uses a required-only bounded
[date plan](../Defaults/Resolvers/RequiredDateTimeRule.cs). Known
date functions, argument counts, offsets and format syntax are checked throughout
the date tree before nested resolver callbacks. Bare clock tokens do not accept
arguments. Custom nested operators have envelope/identifier checks, not advance
semantic qualification of arbitrary plugin dialects. Limits remain 16,384 rule
characters and 32 date-plan levels; required registry depth also applies.

Date literals are invariant Gregorian ISO: yyyy-MM-dd, or
yyyy-MM-ddTHH:mm:ss with optional one-to-seven fractional digits and optional Z
or signed HH:mm offset. Quotes delimit a literal; culture-dependent short dates,
month names, omitted seconds, non-padded components, invalid dates and unsupported
offsets deny rather than resolving to the current clock. No zone gives DateTime
with Unspecified kind; Z gives UTC DateTime; an explicit offset gives DateTimeOffset
with the supplied offset, without converting it to the host timezone.

ADDDAYS/ADDHOURS/ADDMINUTES accept signed invariant decimal text, including quoted
arguments, up to 28 digits/64 characters. Exponents, non-finite values and offsets
that cannot be represented as exact whole 100-nanosecond ticks deny. ADDMONTHS and
ADDYEARS accept signed Int32 integers only. Overflow/range errors deny; month/year
operations retain the platform calendar semantics, including end-of-month clamping.
Arithmetic retains the actual DateTime kind or DateTimeOffset offset. These are
wall-clock/fixed-offset operations, not named-timezone DST validation or scheduling.

FORMAT/DATEFORMAT use the invariant formatter, with 1,024 format characters and a
16,384-character result bound. Malformed standard/custom formats deny rather than
returning an unrelated date string. Formatting does not implicitly reinterpret
the source timezone. Standard formats retain their documented platform semantics.

Nested tokens/functions resolve through the pinned registry with the actual
IPassedArgs context. A nested result must be DateTime or DateTimeOffset: null,
strings, numeric values, TimeSpan, DateOnly and observer objects are not reparsed or
coerced. Reported failures/cancellation stay sticky and prevent destination writes;
earlier acknowledgements remain Partial without replay. Synchronous plugins can
still perform effects or block before returning; this is not callback rollback.

Each bare clock evaluation captures one current UTC instant, derives its local
calendar/time once and preserves UTC/Local kind. Calendar starts/ends are midnight;
week starts Monday and ends Sunday. CURRENTTIME is actual TimeSpan time-of-day and
cannot be used as a date base. Clock leaves are dynamic, not a frozen run instant;
host clock changes and complete immutable policy/time context remain separate work.
Legacy direct calls and custom subclasses retain their semantic contracts outside
this exact shipped required implementation.

Direct calls to the exact shipped resolver with empty rules also report sticky
required failure instead of returning the current clock. Outside required frames,
the legacy empty-rule fallback remains unchanged.

### Shipped Required Datasource Queries

The exact shipped DataSourceResolver now has its own required-only query plan.
GETENTITY/LOOKUP/ENTITYLOOKUP, COUNT/MAX/MIN/SUM/AVG and QUERY's scalar/first/exists/
count/aggregate modes validate all arguments before context conversion or provider
callbacks. Quoted mode/function/entity/field names retain their roles; aggregate
mode accepts MAX/MIN/SUM/AVG, not an ambiguous COUNT(field). COUNT rows uses the
dedicated token/mode. Every trailing argument is a required filter, not ignored
text. Up to 256 filters (260 total arguments) are admitted within rule bounds.

Filters contain one unquoted operator from =, !=, <, <=, >, >=. Scanning respects
quoted names/values, so 'Name=Code'='a!=b' has the intended field/value. Missing
operands, duplicate operators, unquoted whitespace/compound clauses or malformed
quotes deny before reads. Quote punctuation, whitespace and explicit empty values.
Quoted '@Name' is literal; unquoted @Name binds a declared context value, never a
missing-parameter literal. This is an AppFilter grammar, not SQL/free-form query
text or a provider-independent SQL-null language.

Binding order is one case-insensitive named Objects entry, the explicit @FieldName
string slot, then ReturnData's actual flat field. Only when ReturnData is absent
may one named Objects Record supply the row; arbitrary first-object fallback is
not used. Ambiguous/missing/null/non-finite/object/enum/byte-array bindings deny.
Supported closed scalars format invariantly (floating roundtrip, Decimal G29,
date/time roundtrip, lowercase Boolean); no arbitrary observer ToString executes.
Named collections are limited to 10,000 entries, each bound string to 1 MiB and
aggregate filter text to 16 MiB estimated UTF-16 bytes.

A supplied DataSource handle wins; otherwise an explicitly supplied DatasourceName
is resolved once on the supplied editor. Ordinary import row contexts do not
automatically select a source/destination for queries. Hosts must explicitly supply
the intended query context; this boundary does not infer/freeze provider identity.
Null result collections deny, while a real empty collection gives COUNT=0 and
EXISTS=false. Empty first/scalar/extrema/sum/average and scalar null results do not
fabricate required values. Scalar LOOKUP/QUERY results use bounded closed literal
admission and clone byte arrays; FIRST/GETENTITY deliberately return the actual
record object and do not claim deep ownership.

Rows are consumed without ToList, with a 100,000-visited-row bound. FIRST/SCALAR/
EXISTS consume at most one row and dispose; they do not validate the unused tail.
COUNT rejects null rows. Aggregates skip only actual null fields, not missing,
ambiguous, unsupported or malformed fields/values. SUM/AVG require actual numeric
types and reuse the required Decimal/IEEE arithmetic and finite Double result
boundary. MAX/MIN preserve the selected numeric/string/char/date/time field type
and use exact typed ordering, not lossy Double conversion; incompatible values,
Boolean/Guid/blob extrema and overflow deny.

Cancellation/sticky failure and root provider ErrorObject are checked around
callbacks, cursor movement/current/field reads and after disposal. A non-Ok root
flag or nonnull root exception denies even a nonnull collection; denial stops
later diagnostic getters. Null ErrorObject is permitted by the legacy IDataSource
contract, not proof of acknowledged/consistent reads. Callback/enumeration/disposal
exceptions deny safely, preserving earlier acknowledged destination writes as
Partial without replay. Synchronous GetEntity can still block/eagerly allocate;
the engine cannot interrupt it, undo side effects or inspect hidden/nested provider
errors. Shared handles/status, read isolation, live-provider behavior, SQL/filter
translation, query context ownership and plugin subclasses need separate
qualification. Legacy direct and custom query semantic paths remain unchanged.

### Shipped Required Identity And Environment Scope

Exact shipped UserContextResolver/EnvironmentResolver required paths no longer
acknowledge synthesized identity or cross-scope substitutes. USEREMAIL requires an
explicit UserEmail string; USERROLE requires UserRole, while USERROLE(Application)
requires the exact ApplicationRole key and cannot fall back to a generic role or
Windows group. One public instance context property OR one case-insensitive named
Objects entry supplies the value. Missing/conflicting/ambiguous/null/non-string/
blank/control-containing/over-4096-character values deny without ToString coercion.
Named lists are bounded to 10,000 entries; getter exceptions/cancellation deny
without host-role fallback or raw logs. Imported ReturnData and arbitrary first
Objects records are never identity sources. Quoted application names retain their
punctuation and dot normalization retains the declared key.

Explicit strings are supplied host data, not authentication/authorization proof,
and no framework bridge automatically selects a Studio principal or application
identity service. Ordinary import contexts do not automatically provide these
keys. Hosts must supply coordinated context or a separately qualified resolver;
plugin instances/host getters remain trusted mutable code, not owned run identity.

USERNAME/CURRENTUSER/USERLOGIN and USERDOMAIN retain OS account/API meanings, not
an assumed logged-in application user. USERID/USERPRINCIPAL read the actual Windows
SID/name from a disposed identity handle, without username substitution. USERGROUP
returns only verified Administrator, PowerUser or User builtin membership (in that
order), not a claimed primary/application role; no match denies. Windows-only
identity operations deny on unsupported platforms rather than inventing values.

USERPROFILE uses the requested OS special folder; unknown or unavailable folders
deny instead of becoming the profile root. USERPROFILE(TEMP) and TEMP/TEMPPATH use
the actual Path.GetTempPath API, not InternetCache or a raw TEMP string. Downloads
uses the Windows shell's known-folder result, not Profile + Downloads concatenation.
COM initialization/task-memory ownership is balanced, including failure/cancellation
and pre-existing caller apartments. See Microsoft's
[known-folder API](https://learn.microsoft.com/en-us/windows/win32/api/shlobj_core/nf-shlobj_core-shgetknownfolderpath)
and [folder identifiers](https://learn.microsoft.com/en-us/windows/win32/shell/knownfolderid).
This read does not create folders, edit redirection, initialize keys or change host
profile settings. Downloads has no inferred Unix/XDG fallback in this increment.

ENV aliases default to Process and read only the explicitly requested Process/
User/Machine scope; SYSTEM maps to Machine. Missing values cannot fall back across
scopes. SYSTEMPATH now means Machine PATH in required work; USERPATH remains User
PATH. Use ENV(PATH) for the inherited Process PATH. Unsupported persisted-scope
platforms deny. Colon-form quoted keys preserve commas/parentheses instead of being
misclassified as calls. Invalid names, API exceptions or oversized returned strings
deny safely; genuinely empty strings retain value meaning if supplied by the API.

Legacy direct identity/profile/environment methods and custom subclasses retain
their semantic contracts. No declared scope is silently broadened by the exact
shipped required path. Native OS reads are synchronous, cannot be interrupted or
rolled back, and remain sensitive to impersonation/account/environment changes.
This is not complete context capture, group/authentication policy, concurrent
writer coordination, redirected-folder failure injection or all-platform proof.

Resolver instances are associated with the supplied editor through weak-keyed
ownership. Initialize with another editor does not discard custom registration.
DefaultsManager.RegisterCustomResolver still registers the supplied plugin; required
imports use that editor's registry, not the last process-initialized editor.
Registration/selection snapshots are coordinated and CanHandle/ResolveValue
callbacks run outside registry locks. GetResolvers returns a defensive registry
copy. Required imports/direct batches now capture an ordered roster (resolver
instances plus registered priorities) during defaults admission, before source
reads. Both declared sync directions are pinned before provider-opening validation;
when forward has rules, reverse reuses its roster. Replacement, removal or higher-
priority registration cannot change later admitted rows/directions. Fresh top-level
runs/batches capture the updated roster. Standalone ApplyDefaultValues snapshots
enabled definitions and its roster for the call; nested work retains that scope.

The operation context flows through async execution and is restored on completion,
failure or cancellation. Concurrent operations on one editor retain independent
rosters. Nested required calls cannot rebind to another editor, even if a plugin
catches the exception. Resolver SentData is a per-row definition copy; changing
its Rule, PropertyName or IsEnabled cannot alter the retained definitions or the
current assignment's captured field name. Named ResolveDefaultValue/GetColumnDefault
lookups inside required work use the admitted enabled definitions and datasource,
never late catalog lookup. Missing/foreign/undeclared lookups fail; recursive rules
respect the required depth bound. Qualified entity.column keys must be declared
for GetColumnDefault. Only operational definition fields are copied, not custom
subtype/metadata graphs.

Roster ownership is not plugin-instance immutability. Captured instances can still
read changing configuration/environment/provider state or mutate their own internals;
CanHandle truth and callback side effects remain trusted behavior. Other policies,
integration context and metadata/provider handles are not frozen by this roster.
Required registry bootstrap installs the ten built-ins without registration log
observers or partial-success fallback. Direct public manager construction and
explicit custom registration retain their legacy observer behavior.

Required resolution bypasses the legacy metadata-only value cache. A scoped
AsyncLocal diagnostic frame detects BaseDefaultValueResolver errors/warnings and
suppresses its raw rule/value/exception logs during required work. Nested
DefaultsManager.Resolve, resolver-manager ResolveValue (both argument shapes),
ResolveValueWithNormalization, ResolveWithTelemetry and ResolveDefaultValue calls
inherit required semantics while a required frame is active. Nested null/unknown/
exception/reported failures remain sticky even when caught or represented by a
telemetry outcome; they cannot be hidden by an outer resolver returning a value.
Required telemetry omits raw rule/resolver/fingerprint fields and returns a generic
safe error, not the legacy full diagnostic envelope. Selector cancellation/failure
stops subsequent plugin callbacks; tokens are checked around CanHandle and resolve.
The frame is restored on exit and
does not cross-classify simultaneous editor resolutions. Rule length is limited
to 16,384 characters, call-envelope/nested required resolution depth to 32.
These are admission bounds, not a complete legacy grammar, NFEL validator or
executable-plugin security sandbox.

Cancellation is checked before and after resolution and between defaults. It
cannot interrupt synchronous provider/plugin calls or undo a callback mutation.
The failed output exposes only stage/type diagnostics, not raw exceptions or rows.
Required diagnostics do not invoke arbitrary logger observers with raw failures.
Plugins overriding logging or concealing failures entirely remain trusted host
code and need separate conformance; the engine cannot infer their internal truth.

## Counts And Compatibility

Catalog/configuration denial is TransformationAdmissionFailed, not a failed row.
A row resolver failure is RecordsTransformationFailed, included in RecordsFailed
but not WriteAttempts/HasUncertainWrites. Earlier acknowledged rows remain
RecordsSucceeded and yield Partial. Sync does not retry the whole run for these
failures or advance successful cursors/dates, including zero-write failures.
This does not provide row rollback, cursor/schema atomicity or provider recovery.

Public legacy interfaces and method signatures remain. DefaultsManager.Resolve
also now preserves expression prefixes and selects the supplied editor's manager.
Outside required import resolution it retains best-effort fallback/cache/logging
semantics. The wrappers above participate in required semantics only inside an
active required frame; this does not qualify every legacy helper/profile route.
Legacy static accessors/profile/config state remain shared. Retain an editor's
actual manager at a coordinated host boundary rather than using the last-global
ResolverManager accessor in required callbacks; a foreign accessor now denies
required rebinding instead of silently selecting another runtime.
Resolver registration now survives switching editors; hosts that relied on
initialization clearing registrations must unregister explicitly using their
coordinated legacy manager access. Dispose of the legacy facade does not declare
ownership of every editor's registered plugins or drain their background work.

## Evidence And Remaining Gates

ImportDefaultCatalogTests retains nine admission/empty-catalog cases per TFM.
ImportDefaultCaptureTests adds 33 cases; SyncDefaultCaptureTests adds seven:
reused config/catalog refresh, caller/source-enumerator mutation, per-row byte
isolation, 21 actual closed literal types, opt-out, invalid/non-finite/oversized
literals and aggregate budget, both-direction early denial and empty/nonempty
catalog retention with and without a direction edit. Initial 14 capture regressions
failed before implementation; subsequent checks exposed provider-opening validation
ahead of catalog capture, now reordered. These are recording providers, not live
provider/transaction or concurrent catalog writer proofs.
ImportRequiredResolverTests and the sync resolver cases add 34 cases per TFM:
malformed fallback and expression-prefix regressions, real valid values, actual
per-row payloads, retained registrations/cache bypass, scoped safe diagnostics,
concurrent editors, cancellation, partial acknowledgements and retry/cursor gates.
The initial 15 resolver regressions failed before the implementation.

ImportResolverOwnershipTests adds 40 cases and SyncResolverOwnershipTests adds
three per TFM: pre-source/per-row/forward-write registry mutation, fresh-run refresh,
real legacy-cache seeding, nested facade/manager/dictionary/telemetry wrappers,
sticky swallowed failures and raw-log denial, foreign editors, per-row SentData,
standalone definition mutation, failure/cancellation restoration, concurrent runs
on one editor, owned named lookup/recursion, quiet required bootstrap and selector
callback boundaries. All initial 16 ownership cases failed before implementation;
later red probes reproduced three wrapper raw-rule logs and two post-denial callback
cases. See the execution log for final matrix/fixture qualifications.

ImportBuiltInGrammarTests adds 59 cases per TFM: unknown substring tokens, ignored
arity/empty slots, environment target names, sequence placeholders, actual row/config
routing, nested date arguments/quoted format commas, GUID aliases/formats, quoted
comparison operands and colon keys, custom contracts and legacy compatibility.
The initial 42-case regression run had 38 failures/four passes before implementation;
the broader focused import resolver suite now passes 131 cases on net9. See the
implementation log for the final all-TFM matrix and remaining semantics.

ImportRequiredDotRuleTests adds 47 cases per TFM: malformed/missing dot segments,
bare-token failures, preserved scalar/empty string literals, numeric separators/
quoted decimals/aliases, grouped conditions/dates and actual provider filter
arguments, pre-read query denial, custom overrides/unknown dialects and public
legacy compatibility. Its initial 41-case regression run had 31 failures/10 passes;
the combined net9 import resolver suite now passes 178 cases. See the execution log
for that increment's final matrix; it did not close expression/query/provider qualification.

ImportStrictExpressionTests adds 82 cases per TFM: actual Boolean admission, full
syntax/precedence, exact 64-bit/Decimal/floating comparisons, invariant culture,
typed row operands, loss/overflow/underflow denial, Decimal midpoint rounding,
quoted MATH names, inclusive Int32 random endpoints, lazy getter/branch behavior,
pre-read syntax/token/node/depth limits, cancellation after a getter, captured
custom nested math selection and legacy compatibility. The initial 48-case net9
run had 39 failures/nine passes; the combined net9 resolver suite now passes 260.
Recording-provider assertions are not live-provider, plugin-security or package
consumer proof. See the execution log for the full-TFM matrix and remaining gates.

ImportRequiredQueryTests adds 80 cases per TFM: full pre-read query/filter admission,
quote-aware operators/literals, actual invariant row/named-context binding,
null-versus-empty results, root provider failure, typed extrema/date/Decimal
aggregates, invalid/ambiguous/missing values, overflow, binary/DataRow scalar
ownership, bounded streaming, first-row disposal, cancellation, nested expression
composition, custom overrides, safe diagnostics, partial acknowledgements and
legacy compatibility. After fixture qualification, 36 of the original 46 cases
fail on the legacy query path; no assertions were removed. The final combined
net9 resolver suite passes 340. See the execution log for corrected mock fixtures,
full-TFM evidence and remaining query/provider intent gates.

ImportRequiredIdentityScopeTests adds 56 cases per TFM: denial of fabricated email/
role/profile and cross-scope fallback, real host properties/named values, strict
types/ambiguity/limits, no row spoofing/observer conversion, getter failure and
cancellation, exact application keys, actual OS account/SID/membership, temporary/
special folders, readonly known-folder checks in STA/MTA, explicit Process aliases,
Machine/User PATH, colon keys, custom overrides and legacy compatibility. The
qualified original 36-case net9 legacy-path run has 24 failures/12 passes; the
combined resolver suite passes 396. See the execution log for corrected payload
fixtures, full Windows-TFM evidence and remaining identity/provider/platform gates.

ImportRequiredDateTests adds 84 cases per TFM: strict ISO/culture rejection,
invariant formatting, exact fractional/calendar offsets, UTC kind/explicit offset
preservation, arithmetic/format limits, pre-callback known-tree admission, actual
typed nested/property results, pinned overrides, safe failures/cancellation,
partial acknowledgement, all clock aliases and legacy/subclass compatibility.
After fixture correction, the initial 55-case legacy-dispatch run has 22 failures
and 33 passes; the combined net9 resolver audit passes 480. Three additional direct
empty-rule cases fail on the old early-return path before the final fix. Recording tests do not
qualify all plugin dialects, host clocks, timezone databases or package consumers.

ImportRequiredConfigurationTests adds 83 cases per TFM: exact environment/source
namespaces, function/colon/dot aliases, host maps and empty strings, whole-source
type/key/count/size/budget admission, no row/editor inference, connection alias
conflicts, callback/disposal failure and cancellation boundaries, sticky failure,
safe diagnostics, dynamic reads, partial counts and legacy/custom compatibility.
The original 41-case net9 run had 24 failures/17 passes before runtime edits; no
fixture assertions were removed. Final combined resolver audit passes 563 cases.
This is local required configuration qualification, not host bridge/run-owned
configuration, live credential readers or all-route persistence/redaction proof.

Remaining R15 qualification includes complete immutable policy/plugin context,
complete built-in/custom rule semantics, all-route logging,
persisted configuration-failure compatibility, provider fixtures and package users.
Do not mark the broader phase or R15 complete from this increment. See
[transformation outcomes](TRANSFORMATION-OUTCOMES.md) and the framework execution log.

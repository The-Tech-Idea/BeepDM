# Defaults

Default value management and rule resolution subsystem.

Exact shipped required configuration uses explicit named flat string maps or
declared Process prefixes, never inferred editor/row sources or a bare-variable
fallback. Selected maps are fully bounded/captured before key selection; callback,
disposal, ambiguity and cancellation failures deny. APPCONFIG/WEBCONFIG need actual
host maps. This is per-resolution capture, not automatic host bridging or immutable
run configuration. Read defaults admission for namespaces, credential and limits.

Exact shipped required date rules now use ISO Gregorian literals, checked exact
tick/calendar offsets and invariant bounded formatting. Nested date results use
the pinned registry/context and must be actual DateTime/DateTimeOffset values;
their kind/offset is retained. Clock leaves are dynamic, not a run-owned instant.
Read [defaults admission](../Importing/DEFAULTS-ADMISSION.md) for precision, culture,
format and timezone limits and legacy/custom separation.

For required import resolution read
[defaults admission](../Importing/DEFAULTS-ADMISSION.md). Required imports use an
editor-owned registry, one normalization and no metadata-only result cache; reported
error/warning fallbacks deny writes with safe outcomes. Legacy direct APIs remain
best-effort outside that scope. Imports own closed default literals without changing
caller configuration; sync captures both declared catalogs before provider-opening
validation. Nonblank rules omit unused PropertyValue from captured SentData. Other
static profile/config state is not isolated; full run/plugin intent remains open.
Required operations pin ordered resolver rosters before source reads and preserve
them through both sync directions/nested calls. Registrations affect fresh runs,
not admitted rows. Per-row SentData and admitted named lookups cannot mutate retained
definitions or reread catalogs. Scoped wrappers keep caught failures sticky and
omit raw telemetry; outside required frames they retain legacy behavior. Captured
plugin instances are trusted mutable code, not a security/immutability sandbox.
Required shipped routing uses exact operator tokens and validates outer arity/
empty slots without ignoring extra arguments; nested date argument splitting is
supported. Time/hash SEQUENCE/INCREMENT demonstrations are denied for required
rows; provide a qualified allocator plugin. Read defaults admission for compatible
GUID aliases, dynamic environment/config values and remaining query/identity gates.
Required-only dot normalization preserves literal/empty-string quotes and rejects
missing segments; public legacy normalization is unchanged. Outside quotes/nested
calls dots separate arguments; quote decimals. Grouping quotes apply only to
declared expression/mode/filter positions. Read defaults admission for the exact
roles, custom overrides and numeric-helper compatibility limits.
Exact shipped required expressions/formulas now parse a bounded AST before field
reads, with Boolean-only conditions, precedence, typed exact comparisons, invariant
numbers and lazy branches. Decimal rounding retains midpoint precision; nested
custom overrides use the pinned roster. Unused branches are syntax-checked, not
semantically prequalified. Legacy direct/custom APIs are not generally rewritten.
Read defaults admission for closed scalar admission, numeric/result bounds and
remaining policy/plugin/provider gates.
Exact shipped required datasource queries now validate every filter before reads,
bind closed invariant actual row/context values and distinguish null from real
empty collections. Typed aggregates do not skip malformed values or collapse
64-bit/date extrema to Double; bounded streaming checks cancellation/root provider
failure and disposal. Scalar byte results are copied; FIRST returns actual records.
Query handles/status and live-provider translation are not frozen or acknowledged
by this boundary. Read defaults admission before selecting a provider or extending
query syntax; legacy/custom query semantics remain separate.
Exact shipped required identity/scope defaults now require explicit string email/
application-role context, never row identity or a fabricated/group fallback.
Conflicts, invalid values and getter failure/cancellation deny. OS SID/name/group
and named folders use actual supported APIs; Downloads honors the Windows known
folder. ENV reads only its declared scope; SYSTEMPATH is Machine PATH and ENV(PATH)
is Process PATH in required work. Legacy/custom semantics stay separate. Read
defaults admission for OS/platform/resource limits and why supplied identity strings
are not authorization proof or immutable run context.

## Core Components
- `DefaultsManager` (static facade)
- `Helpers/DefaultValueHelper`, `DefaultValueValidationHelper`
- `Resolvers/*` (rule/value resolvers)
- `Interfaces/IDefaultValueInterfaces`

## Capabilities
- Get/save datasource-level default values.
- Resolve static or rule-based values at runtime.
- Validate defaults and rule syntax before execution.
- Register custom resolvers for project-specific rules.
- Provide templates for common domains (audit, user, inventory, etc.).

## Runtime Use
- Initialize via `DefaultsManager.Initialize(editor)`.
- Resolve with `ResolveDefaultValue(...)` in import/mapping/unit-of-work flows.
- Persist using `SaveDefaults(...)` so settings survive restarts.

## Integration
- `DataImportManager` automatically loads defaults when configuration enables `ApplyDefaults`.
- Mapping and UOW workflows can apply the same resolver pipeline for consistency.

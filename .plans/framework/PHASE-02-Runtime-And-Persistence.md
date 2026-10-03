# Phase 2: Runtime And Persistence

Required defaults follow-up: editor-owned resolution added 34 cases per TFM;
run-owned catalog/literal capture adds 40 more, followed by 43 roster/nested-context
cases, then 59 outer-grammar, 47 dot-literal, 82 strict-expression, 80 query and
56 identity/scope, 84 date and 83 configuration cases.
Final Windows matrix: 4,719 passes, zero failures/skips, 1,240 reliability
cases each on net8/net9/net10. Caller-safe
implicit refresh, closed bounded literals/per-row byte copies and both declared
catalogs before provider-opening validation are verified. Admitted resolver rosters,
per-row definitions and nested required failure/cache boundaries are also tested.
Exact shipped routing/arity, nested dates, quoted keys/operands and GUID aliases
are qualified; required sequence demos deny rows. Required dot literals/empty strings
and grouping/filter roles are tested. Exact shipped required expression/formula
ASTs qualify Boolean-only conditions, precedence, typed exact comparisons,
invariant numbers, Decimal rounding, lazy branches and pre-read limits/cancellation.
Nested overrides retain pinned selection; unused branches are syntax-checked only.
Shipped required query/filter plans now qualify actual closed invariant binding,
null/empty distinction, typed aggregates, safe provider/cursor/disposal failure,
cancellation and bounded consumption with retained Partial acknowledgements.
Query handles/context, allocation, hidden failures, isolation and translation are
not qualified. Required identity/scope core now uses explicit string email/role,
actual supported OS identity/folders and exact ENV scope, with no fabricated/row/
generic/group substitutes. Host strings are not authorization proof; identity/
context/platform, NFEL/numeric semantics and complete policy/plugin/provider
intent remain open. Public legacy parsing is unchanged.
Required date core now qualifies ISO literals, exact tick/calendar offsets,
invariant bounded formatting and actual typed nested values through the pinned
registry. UTC kind and explicit offsets remain intact; clock leaves are dynamic,
not owned run time or named-zone DST qualification.
Required configuration core now qualifies explicit flat string sources/declared
Process prefixes, complete bounded map capture, exact aliases and callback/failure
guards without inferred row/editor values or ambiguous connection aliases. Capture
is per resolution; immutable run configuration and host/credential bridging remain.
Read Engine Importing/
DEFAULTS-ADMISSION.md and IMPLEMENTATION-LOG.md for full immutable policy/plugin,
grammar/provider and wider gates. No phase checkbox changes from this increment.

Latest file-recovery increment: durable reject IDs, operator preparation/CAS claims,
typed payloads, corrupt/unresolved clear protection and acknowledged row replay
now have 55 new cases per TFM; full Windows matrix passes 2,868 executions.
This does not close native adapters, provider-run ownership, cursor/Completed
agreement, other config writers, retention, host keys or Unix gates. P2-05/06 stay open.

Status: in progress. Findings: F04, F05, F06. Depends on Phase 1 result semantics.

Implemented persistence increment: shared AtomicFileStore, JsonLoader failure
propagation/shared reads, coordinated connection catalogs and import JSONL stores,
and versioned typed file watermarks. Windows net8/net9/net10 tests include
separate processes, killed lease owners and concurrent snapshot readers.
See IMPLEMENTATION-LOG.md; this does not complete the phase acceptance gates.

Migration storage increment: additive acknowledged ConfigEditor history methods,
strict corruption/identity validation, coordinated append and preserved legacy
promotion. Mandatory execution checkpoints block DDL on save failure and stop
later work after post-DDL failure with explicit reconciliation requirements.
Sync storage facades no longer block on async I/O; lease-held critical sections
do not suspend. Process restart/append and bounded-pool mixed catalog tests pass
on Windows net8/9/10. P2-05 remains open for other configuration/sync stores,
cross-OS qualification, backup/recovery and remaining public adapters.

Verified runtime increment: AddBeepRuntime/provider-created graphs, per-runtime
defaults, editor-owned single-flight datasource creation, name/GUID invalidation
and cleanup, close-result mapping and coordinated runtime cleanup. The missing
test namespace is fixed. Windows solution verification passed 1,030 executions;
40 runtime/lifecycle cases run on each configured TFM. See IMPLEMENTATION-LOG.md
and Services/RUNTIME-OWNERSHIP.md for evidence and explicit compatibility limits.

## Work Items

Connection increment: optional IConnectionConfigurationPersistence acknowledgement,
captured injectable protection, strict encrypted fallback/catalog updates, version
2.0 packages and key-independent redacted exports are implemented. Catalog mutations
cannot overwrite unreadable key evidence. Post-commit observers are isolated outside
scope locks with separate sanitized diagnostics. Preceding connection Windows matrix: 102 added cases
per TFM, 264 reliability cases per net8/9/10; full solution 1,791 executions.
See Security/README.md and IMPLEMENTATION-LOG.md. P2-05/07/08 stay open for
broader stores/routes, real host key management, Unix and lifecycle qualification.

Current sync storage increment: complete per-schema updates use AtomicFileStore;
versioned envelopes preserve closed typed cursor values; exact-ID hashed artifacts
reject foreign/corrupt state; immutable versions and run-owned clearing preserve
recovery evidence. Startup and completion require checkpoint acknowledgement.
Persisted schema IDs cannot be invented by constructors/coerced from non-strings.
Terminal failure retains counts and requires reconciliation; advisory/property/
subscription diagnostics cannot reclassify saved outcomes. The Windows solution
passes 2,049 executions, including 350 reliability cases each net8/9/10. There are
64 sync storage and 22 added outcome cases per TFM since the connection baseline.
It is not all-platform qualification or phase completion.
The subsequent strict transformation matrix passes 2,136 executions, 379 reliability
cases per TFM. Its 29 additional cases qualify pre-write outcomes, not remaining
storage/secret/platform or full sync recovery guarantees.
Current review R12-R14 retain required gate failures, durable cursor agreement
and recoverable schema/version promotion as open gates.
Durable watermark/schema commit, offset replay and cross-process run ownership
are not established by atomic file updates. Remaining config facades still need
failure/corruption propagation. See ENHANCEMENT-ROADMAP.md for delivery order.

- [x] P2-01 Add tests for two independent service providers with distinct options,
  concurrent scopes, transient lifetimes, and disposal of one provider.
- [x] P2-02 Remove process-wide mutable service reuse from normal registration.
  Define container/scope ownership; share only explicitly immutable registries.
- [x] P2-03 Make datasource check/create/dispose coordination safe. Document the
  concurrency guarantees of mutable editor state and legacy public lists.
- [x] P2-04 Introduce atomic file persistence: serialize before touching the
  destination, write in its directory, replace safely, and propagate failure.
- [ ] P2-05 Apply the persistence contract to config, import errors, and cursors.
  Define same-process and cross-process coordination, backup and recovery rules.
- [ ] P2-06 Version cursor envelopes and preserve timestamp/sequence/composite
  types. Validate schema/datasource identity and collision-safe storage keys.
- [ ] P2-07 Audit connection-secret write paths, especially JSON fallbacks.
  Establish a consistent protector abstraction with explicit non-Windows policy.
- [ ] P2-08 Audit ownership and unsubscribe/dispose paths for datasources, plugin
  runtimes, service facades, and long-running import/sync operations.
- [x] P2-09 Correct name/GUID close outcome mapping: Closed is success, not Open.
  Test missing connections, failed close, repeated close, removal and disposal.
- [ ] P2-10 Define and verify timer-driven lifecycle shutdown for Forms and plugin
  health monitoring: callback overlap, replacement races, cancellation, draining,
  event exception reporting and host/UI dispatch policy. Preserve existing error
  handling; do not infer drain guarantees from Timer.Dispose alone.

P2-05/P2-06 have partial implementation, not completion: legacy manager-level
acknowledgements, broader store coverage, cross-OS behavior and end-to-end BeepSync
commit/promotion durability still need audit. Import legacy files are preserved/rejected explicitly;
automatic migration, backups and scalable retention are not implemented.

The 2026-10-03 sync increment acknowledges Failed checkpoints with typed actual
counts under admitted identity, including cancellation cleanup. Failed artifacts
are immutable/restart-blocking and corrupt evidence is preserved; 58 new cases per
TFM cover thresholds/failure persistence. This does not close P2-06's cursor/schema/
Completed agreement or establish durable reject replay/provider-run admission.

P2-02/P2-03 cover normal registration and editor-routed lifecycle operations,
not universal thread safety for direct CRUD/public list mutation. Explicit legacy
static APIs remain documented single-host compatibility paths. A pending legacy
constructor is invalidated but not forcibly interrupted/drained by Dispose.
P2-08/P2-10 remain open for broader editor/plugin/subscription and timer audits.

## Implementation Boundaries

- Registration changes must cover static EnvironmentService/configuration state,
  fluent builders that eagerly return a runtime, and legacy convenience APIs.
  Do not fix only the factory lambda while leaving shared mutable ownership.
- Persistence coordination must cover the complete read/modify/write operation,
  not only atomic file replacement. Test mixed synchronous/asynchronous catalog
  calls, multiple instances, processes, cancellation and corrupt-file recovery.
- Use closed tagged cursor types, not arbitrary type names from untrusted JSON.
  Define old-format conversion/rejection and consistent case/canonicalization
  rules for storage identity, including compatibility with existing stores.
  Typed storage does not automatically enable new BeepSync execution modes.
- Audit raw ConnectionString and ParameterList as well as named secret fields.
  Non-Windows/key failures must be explicit and must not fall back to plaintext.
- Inventory direct environment and BeepSync schema/version/checkpoint writes.
  Coordinate whole load/change/save operations, not just file replacement.
  Empty/corrupt existing files must not masquerade as fresh empty configuration.
- Preserve a catalog Save(false) result through the configuration facade. Add
  observable outcomes without breaking existing void-return callers; define
  legacy failure mapping rather than merely logging and continuing.
- Migration history must distinguish missing from unreadable/corrupt state.
  Coordinate complete append/update operations and preserve corrupt evidence;
  atomic replacement alone cannot prevent lost records. Define collision-safe
  datasource identity and a deliberate legacy-filename compatibility policy.
- Carry mandatory checkpoint persistence outcomes through the config facade and
  migration writer. Block initial DDL on failed start-checkpoint persistence;
  failed saves after acknowledged DDL require reconciliation, not blind retries
  or a durable-success claim. Preserve the primary operation error when secondary
  diagnostic/cleanup persistence also fails. See CURRENT-REVIEW.md R10.
- Require acknowledged sync terminal persistence as well as startup. Separate
  post-commit observers from outcomes and stage promotion before live publication.
  Define recoverable schema/version/cursor agreement; a per-file lease alone does
  not create a multi-artifact transaction or an execution admission lease.
- Protect or strip credential-bearing ConnectionString/ParameterList content on
  catalog and export paths, not only named secret properties. Test sentinel
  secrets in persisted bytes, exported packages and logs, including key failures.
- Timer callbacks must carry generation identity rather than only a mutable
  name lookup. Test queued callbacks from replaced timers and reject creation
  after shutdown; specify synchronous versus asynchronous drain behavior.

## Verification And Acceptance

- Containers and scopes use their own configuration and connections; disposing
  one cannot invalidate another container's runtime.
- Serialization failure or interrupted replacement preserves the last valid file
  or a documented recoverable backup; failures are observable to callers.
- Concurrent writers/readers are tested across instances and processes, not only
  through one instance semaphore. Validate Windows and supported Unix behavior.
- Restarted cursors preserve their value type and identity. Legacy files have an
  explicit conversion or rejection path, never silent guessed interpretation.
- Protected secrets remain protected across every supported persistence route;
  key availability errors do not silently fall back to plaintext.
- Timer shutdown tests control an already-running callback; prove the documented
  post-shutdown behavior and no accidental overlap for non-overlapping schedules.

Next: [provider and migration contracts](PHASE-03-Provider-And-Migration-Contracts.md).

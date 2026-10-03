# Phase 1: Write Correctness

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

Status: in progress. Findings: F01, F02, F03, F06. Highest-priority delivery phase.

Implemented 2026-10-02: write acknowledgements, deferred transactional acceptance,
safe automatic retry boundaries, cancellation/cursor gates, native sink upsert
capability, and opt-in sink transactions. See [execution log](IMPLEMENTATION-LOG.md).
Live-provider recovery and idempotent manual resume remain acceptance gates.

Planning refresh: R14/R15 expose a separate pre-write admission gap. Completed
P1-05/06 accounting work remains delivered; it does not prove that configured
quality rules execute or required transformations finish. Address the following
under existing work IDs rather than introducing a duplicate completion checklist.
The strict transformation core is now verified with 29 added cases per TFM and
2,136 total Windows solution executions. Quality admission, implicit catalog/resolver
qualification remain open. Existing-target mapped sync subsequently has 27 new
cases per TFM. Record admission subsequently adds 51 cases per TFM for actual
normal/bidirectional writes, captured modes, defaults, stores and combined counts.
Attempt thresholds and failed checkpoint counts now add 58 cases per TFM. Durable
reject/operator recovery and mapped creation/provider gates remain.
See CURRENT-REVIEW R14-R16; no broader recovery checkbox is completed.

Subsequent durable file reject recovery adds 55 cases per TFM: run/reject identity,
typed destination snapshots, operator preparation/CAS claims and actual import/
sync replay acknowledgements. Uncertain/claimed rows stay blocked; row replay
does not complete failed runs or advance cursors. Latest full Windows matrix:
2,868 passes, zero failures/skips. Native/live-provider and provider-run recovery
keep P1-10/11/12 open; defaults/resolvers and mapped creation remain.

## Work Items

- [x] P1-01 Diagnose the setup full-install failure using the returned error and
  requested transport URLs; repair the fixture or service based on evidence.
- [x] P1-02 Add UOW regression tests for one failed record, transaction commit
  failure, rollback failure, generated keys, deletes, and edits during save.
- [x] P1-03 Separate provider write acknowledgement from OBL acceptance. Preserve
  or restore tracking state until database commit is confirmed.
- [x] P1-04 Make transaction completion/cleanup and AfterSave event timing
  explicit. Preserve the original failure if cleanup also fails.
- [x] P1-05 Add typed import outcomes with attempted/succeeded/failed/skipped
  counts and cancelled/partial/failed terminal states; map legacy Errors safely.
- [x] P1-06 Count acknowledged writes rather than batch size. Define automatic
  retry and skip policies that do not replay acknowledged/uncertain writes.
  Manual recovery and durable diagnostics are tracked separately below.
- [x] P1-07 Make ETL sink failures reach engine policy. Do not insert after an
  arbitrary update failure. Expose transaction support rather than no-op promises.
- [x] P1-08 Stop sync success/checkpoint bookkeeping on failure or cancellation;
  put temporary event-handler and resource cleanup in finally paths.
- [x] P1-09 Reject unsupported cursor modes or implement typed mode-specific
  cursor comparisons; advance only under the defined committed-work policy.
- [ ] P1-10 Define manual restart/reconciliation after partial or uncertain
  writes. Persist stable run/record identities and use provider-backed replay
  keys where available; do not promise universal exactly-once execution.
- [ ] P1-11 Add live-provider tests for generated identities, write failures,
  commit/rollback failures, and reconnect/restart behavior. Record tested versions.
- [ ] P1-12 Integrate durable per-record diagnostics/error-store replay tests;
  verify that accepted rows are not replayed and unresolved writes stay visible.

## Verification

Exercise the concrete DataImportTransformationHelper, not only the identity mock
used by ImportWriteTests. Required filtering/mapping/default/custom-stage failures
must deny the affected write. Ordinary QualityRules and both sync record directions
now execute after transformation and before provider invocation with local evidence.
Prove actual reject counts, required/advisory behavior, missing engine/rule failures,
malformed/throwing/timeout outputs, reject-store failures and cancellation. A mixed
run retains earlier acknowledgements without false Success or cursor advance.
Document legacy transformation fallback and any intentional opt-in advisory stages.

Create focused UOW/import/pipeline tests using recording datasources capable of
returning Failed without throwing. Include a real transactional provider fixture
to verify rollback rather than assuming fake behavior represents database state.
Cover all-failed, mixed-result, retry exhaustion, cancellation during a batch,
update-not-found versus connection failure, and equal-value cursor boundaries.

## Acceptance

- A rolled-back transaction leaves recoverable dirty state and no false success
  events; a confirmed commit accepts exactly the committed changes.
- All-failed batches never report success. Counts distinguish attempts from
  committed writes, including retried and deliberately skipped records.
- Failure/cancellation never advances a sync cursor or publishes Success.
- Unsupported CDC modes fail before target writes; supported modes retain their
  value type and ordering semantics.
- Existing public callers keep compiling through documented compatibility adapters.

Next: [runtime and persistence](PHASE-02-Runtime-And-Persistence.md).

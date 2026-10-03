# Phase 3: Provider And Migration Contracts

Migration ownership backend increment: Models adds optional
IMigrationExecutionOwnership and immutable claim observations; ConfigEditor exposes
the local FileMigrationExecutionOwnership adapter. Permanent live owner handles,
durable abandoned-work detection, explicit expected-claim reconciliation and
immutable released evidence archives are implemented. Read Engine Migration/
EXECUTION-OWNERSHIP.md. MigrationManager does not yet acquire this capability:
execute/resume/compensation/imperative integration, scoped checkpoint authority,
defensive progress and provider intermediate-state recovery keep P3-06/08 open.
Final source-built Windows matrix: 5,178 passes, zero failures/skips; 54 added
backend cases and 1,393 reliability cases per TFM. Initial qualification failures
are retained in IMPLEMENTATION-LOG.md; no manager/provider recovery claim.

NFEL-1 core follow-up: 99 new cases each on net8/net9/net10 qualify complete
grammar, actual SolveRule/direct results, supplied source/token admission, owned
policy/lifecycle, typed/lazy evaluation and bounded defensive history. Fresh Windows
matrix: 5,016 passes, zero failures/skips; 1,339 reliability cases per TFM.
Read Engine Rules/NFEL.md. Other dialects/profiles/adapters/helper/provider and
full phase gates remain; P3-09 is not newly checked complete.

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

Latest R14 file-recovery increment qualifies actual import/sync direction replay
under current record policy with 55 new cases per TFM. Claimed/uncertain rows
require explicit reconciliation, not retries or fabricated success. This is not
live-provider conformance, native-channel triage, full governed intent, provider-run
ownership or cursor/promotion agreement. Existing open work IDs remain unchanged.

Status: in progress. Findings: F03, F07; current R02, R08, R13-R18.
Depends on the implemented Phase 1/2 result and persistence contracts; independent
provider inventory and migration-integrity work need not wait for all open gates.

Current increment: operation-level Models snapshots, version 2 canonical identity,
private snapshot preview/execution, explicit policy/governance revisions, hash-bound
approval options, persisted plan/checkpoint payloads, legacy rejection and captured
rollback intent. UpToDate dependencies and continue-on-failure terminal outcomes
are corrected. Migration regressions now run on all three configured TFMs.
See IMPLEMENTATION-LOG.md and Editor/Migration/PLAN-INTENT.md for verification
and compatibility. P3-04 snapshot persistence is now acknowledged and tested on
the local Windows/process matrix. P3-06 remains open for concurrent-token
admission and provider-confirmed intermediate-state recovery;
do not interpret baseline rejection of partial DDL as universal resume support.

## Work Items

- [ ] P3-01 Inventory actual provider support: transactions, async cancellation,
  paging/streaming, bulk writes, native upsert, schema operations, and DDL rollback.
  Separate SQL-generation support from verified runtime capability.
- [ ] P3-02 Introduce optional capabilities and consistent write outcomes without
  breaking all existing IDataSource implementations at once.
- [ ] P3-03 Build reusable provider conformance tests with disposable file and
  relational fixtures. Cover failure acknowledgements, schema refresh, keys,
  transaction boundaries, and ownership.
- [x] P3-04 Persist a canonical desired-schema and execution-context snapshot in
  migration plans. Include field type/nullability/size/keys/defaults and relations.
- [x] P3-05 Version PlanHash; make meaningful schema changes alter its identity.
  Replace the existing insensitive-create-column characterization with regression
  tests; prove irrelevant ordering does not change canonical hashes.
- [ ] P3-06 Execute/resume the approved snapshot, verify current target state, and
  reject stale approvals/checkpoints. Define handling of previous hash versions.
- [ ] P3-07 Document routing between SchemaManager, imperative migration helpers,
  and governed plans so callers cannot accidentally bypass required checks.
- [ ] P3-08 Test foreign-key cycles, dependency ordering, unsupported operations,
  irreversible DDL, and compensation failures using actual provider capabilities.
- [ ] P3-09 Add targeted helper/rule conformance tests: dialect quoting, type and
  nullability mappings, unsupported feature diagnostics, deterministic expression
  evaluation, malformed input and recursion/size limits. Separate pure generation
  tests from live execution. Audit registered rule/plugin side effects rather than
  assuming expression parsing provides a sandbox.

## Verification And Acceptance

- Different create-table column sets no longer share PlanHash. Field-definition,
  constraint, and applicable execution-policy changes affect the approved identity.
- Resume rejects incompatible schema intent even when entity names are unchanged.
- Plans expose unsupported operations before execution and do not imply DDL
  rollback or compensation guarantees absent from the provider.
- Conformance results identify the exact provider/version tested; generating valid
  SQL alone is not reported as successful database execution.
- Existing persisted checkpoint reload and opt-in destructive migration controls
  remain covered. Reconcile existing migration roadmaps rather than duplicating
  capabilities already implemented.

## Delivered Intent Slice: P3-04/05/06

Schema capture, canonical identity and approved-snapshot execution landed
together; P3-05 is verified. Cloned field/constraint definitions and execution
context are serialized in plan/checkpoint records. Hashes exclude timestamps,
diagnostic wording and lifecycle state while retaining semantic key/index/relation
column order. Legacy intent is rejected and changed policies require new approval.
The subsequent acknowledged local storage increment is recorded below; neither
slice establishes full provider recovery or cross-process execution admission.

Existing regressions cover field definitions, index/relation payloads, culture/
ordering stability, metadata/target/policy mutations, stale tokens and recording
history reload. Keep provider-payload assertions and legacy rejection coverage;
recording fixtures alone do not prove durable storage or live DDL behavior.

## Sync Governance And Rules Follow-Up

Current review R13/R14 also apply to P3-07/09: the sync promotion fingerprint is
not the migration intent hash and does not yet bind full keys/filters/policies.
Stage approvals and define recoverable artifact publication before advertising
durable governance. Required DQ rules must distinguish pass, reject and evaluation
failure; errors after provider writes require partial-work/reconciliation evidence.
Test malformed/throwing/timeout rule results separately from advisory observers.
Normal-import and forward/reverse record admission now have 51 new cases per TFM:
captured record policy/context, typed decisions, actual generated/defaulted rows,
required/advisory errors, store failures and combined acknowledgement evidence.
The dedicated gate is not CustomTransformation or replay-only evaluation. Preserve
these delivered boundaries. Required/advisory attempt thresholds, real/empty
denominator, strict action outputs and acknowledged failed counts now add 58 cases
per TFM. Durable reject/operator recovery remains proposed; see CURRENT-REVIEW
R14/R15 and roadmap B1. Custom rule/store implementations, live-provider commit
and complete immutable governance are not qualified by this local slice.
NFEL-1 complete grammar/execution and bounded retention are now locally qualified;
other parser/profile/wrapper/package conformance and workload gates remain open.
Strict transformation core now has 29 new cases per TFM; default/resolver and
custom-helper qualification remain. R16 existing-target mapped binding now has
27 cases per TFM with actual generated forward/reverse payloads. Both target field
lists use captured provider metadata; mapping-aware preflight/validation checks
renames, exact key pairs, unique targets, required coverage and actual generated
shapes before either writes.
Bound mode refuses missing targets rather than inferring mapped DDL. Mapped creation,
immutable policy/context, native upsert and provider qualification remain open.
R17 source-sensitive generation and exact type selection now have 26 new cases
per TFM, plus existing strict mapping tests changed to actual generated targets.
Controlled cache races and annotation/schema changes are verified; package consumers,
other helper/generator routes and noncollectible assembly lifecycle stay open.
R18 field cloning and Models EntityMetadataSnapshot.Capture are now verified with
27 cases per TFM: merge/copy callers, aliases, nested mutation, observer isolation
and supported/rejected graphs. EntityStructure's legacy Clone stays shallow.
Existing-target sync now binds owned metadata; wider run-policy admission and
package compatibility remain open. See roadmap B1 for dependencies;
these are additional acceptance criteria under P3-07/09, not new completion IDs.

## Acknowledged Storage Increment: P2-05 With P3-04

Acknowledged history/plan/checkpoint storage is implemented as described in
CURRENT-REVIEW.md R10. Tests prove failed initial saves cause zero DDL, failed
post-DDL saves expose acknowledged work and reconciliation requirements, corrupt
history is preserved, concurrent appenders retain records and reload survives a
separate process. Existing IConfigEditor signatures remain; optional capabilities
are required for governed execution, and legacy history void saves now propagate
failures. Broader P2-05 stores, Unix qualification and backups remain incomplete.

The durable backend above is a prerequisite, not automatic token admission.
Next integrate execute/resume/compensation and owned checkpoint progress. Normalize
provider metadata and model exact expected intermediate state after partial DDL,
including uncertain outcomes and dependency ordering. Reconcile actual database
state before replay; the original-baseline guard alone may reject our own applied
changes. Validate host policy-revision adapters and package-only consumers before
claiming compatibility or recovery completion. P3-06 remains unchecked.

For P3-09, add NFEL unmatched-text/grammar regressions (for example `1 @ 2`),
malformed nesting and explicit input/depth limits. Preserve intentional dialect
differences; do not imply that tokenization or registered executable rules provide
a security sandbox. Retention is also tracked by P4-07.

The [post-configuration planning review](REVIEW-AND-PLAN-2026-10-03.md) reproduced
five actual net10 parser/evaluator failures: unmatched text executes, subtraction
and ternary throw, quoted string comparison is wrong and reversed parentheses
parse successfully. Add evaluated-value assertions through SolveRule and direct
evaluation, not only parse rejection. Qualify pre-populated/mutated structures,
policy restrictions, lazy branches, complete consumption and bounded history while
preserving other parser dialects and serialized enum ordinals. These probes are
not automated cross-TFM qualification themselves. The subsequent NFEL-1 increment
now adds 99 actual regression cases per TFM, including all five probe failures,
policy/lifecycle and bounded owned history. P3-09 and P4-07 remain unchecked for
their broader helper/parser/adapter/provider/workload requirements.

Next: [performance and observability](PHASE-04-Performance-And-Observability.md).

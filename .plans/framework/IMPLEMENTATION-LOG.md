# Framework Implementation Log

## Planning-Only Current Worktree Review - 2026-10-03

User requested framework review and an enhancement plan, not runtime implementation.
[REVIEW-AND-PLAN-CURRENT.md](REVIEW-AND-PLAN-CURRENT.md) records the current partial
migration integration, targeted broader source findings, existing-ID delivery
boundaries, dependencies and acceptance gates. Review/roadmap/master entry points
link to it; historical implementation evidence is preserved. No framework source,
skills or completion checkboxes were changed by this pass.

Fresh command: `dotnet build DataManagementEngineStandard/DataManagementEngine.csproj
-f net9.0 --no-incremental -p:GeneratePackageOnBuild=false
-p:GenerateDocumentationFile=false -v:q`. Windows SDK 10.0.401, exit 1, 1,987 warnings,
five errors: ExecutionOrchestration CS1503 at 528 and CS0029 at 600/887/925;
RolloutGovernance CS0120 at 270. Log:
`C:/Users/f_ald/AppData/Local/Temp/BeepDM-framework-planning-build-net9.log`.
No tests were run against stale binaries; other TFMs/full solution/package/provider
qualification was not rerun. Previous green matrices do not qualify this worktree.
Tracked Setup bin/obj remained unchanged; pre-existing source/docs.xml changes were
not reverted. Full phase scope remains unfinished.

Planning validation: all local links and line anchors in the new review resolve;
five planning entry points link to it and `git diff --check -- .plans` passes.
The broader tracker link scan found seven existing missing editor-managers targets
in the unchanged top-level tracker body; they were not repaired in this review.

## Durable Migration Ownership Backend - 2026-10-03

Previous goal turn classification: progress. The planning review refreshed the
authoritative next slice and verified the full source-built solution. This turn
implements the durable admission prerequisite, not a process-local substitute.
The full five-phase objective remains active; no broad checkbox is completed.

### Implemented Boundary

- Models adds optional IMigrationExecutionOwnership, readonly claim/admission
  observations and IMigrationExecutionLease. No existing IConfigEditor/IDataSource
  member was broken or Engine dependency introduced into Models.
- FileMigrationExecutionOwnership uses a permanent exclusive owner handle plus
  coordinated version-one claim snapshots. Same/different tokens and plans conflict
  under the same canonical target/root; independent targets can own separate claims.
  Target keys are hashed; the host must supply stable physical identity across aliases,
  credentials and runtime IDs. Separate roots/spellings are not coordinated.
- Only acknowledged Finish changes durable disposition. Dispose/exception/crash
  leaves unfinished evidence; no timeout or PID inference clears it. Explicit
  expected-claim reconciliation requires actor/evidence and cannot clear a live owner.
  Completed/SafeToRetry release; RequiresReconciliation stays blocked. Unknown,
  repeated, disposed and changed-revision transitions fail safely.
- Before replacement, archive the previous released/operator decision immutably.
  Conflicting/archive-failure evidence blocks a new owner without replacing current
  bytes. Corrupt/foreign/unknown JSON, version/state/revision/time/evidence mismatch,
  oversized inputs, cancellation and replacement denial are observable. Safe errors
  do not expose raw targets/paths/payloads/nested storage messages.
- ConfigEditor exposes the capability at Config.ConfigPath/Migrations/ExecutionOwnership;
  absent/empty roots fail rather than falling back to the working directory. The
  owner lease retains its captured root/claim; host root pinning remains required.
- Added EXECUTION-OWNERSHIP.md, runtime/persistence routing, canonical/harness and
  direct/nested installed migration skill/reference updates. Four skill metadata
  validators pass; ownership guidance blocks agree. .agents copies are untouched.

MigrationManager does not yet acquire this capability automatically. Next integrate
execute/resume/compensation, scope private checkpoint authority and expose defensive
public progress, then qualify provider-confirmed intermediate DDL state. Existing
static mutable checkpoints and competing-manager race remain open. No live-provider,
Unix/network storage, package-consumer or universal power-loss guarantee is claimed.
Initial file reads still materialize before envelope validation; archive retention,
exports and signed/operator audit policy remain P4/P5 qualification work.

### Verification And Fault Evidence

54 new tests per TFM cover actual owner handles, immutable observations/archives,
all saved dispositions, token/target conflicts, root validation, cancellation,
concurrent Finish, revision tampering/overflow, strict corrupt envelopes, replacement
failure and live/killed child-process ownership plus actual operator readmission.
The Windows replacement-denial branch is not exercised on other OSes.

Initial net9 tests: 41 passed, 2 fixture failures. ConfigEditor needs non-null logger/
ErrorsInfo; xUnit InlineData transport replaces an unpaired surrogate, so construct
that value in a Fact. An intermediate fixture edit used the wrong ErrorsInfo namespace;
the build error was corrected. Expanded net9 boundary run passes 52 tests; archive
faults add two more per TFM. No production outcome was weakened to satisfy fixtures.

First complete matrix: 5,175 passed / 3 failed. Existing datasource barrier timed
out and two Setup stderr-drain fixtures exceeded their waits. Isolated lifecycle
rechecks pass all TFMs; both Setup cases pass unchanged. Second full run: 5,177
passed / 1 failed; the new crash fixture observed Busy immediately after confirmed
child exit. Its bounded sidecar-settle observation now requires no lease, unchanged
claim identity and eventual RequiresReconciliation before operator release. It
does not clear durable state, loosen live exclusion or assert process exit alone
proves filesystem handle availability. Scheduling stability remains a broader CI gate.

Final forced rebuild: exit 0, 6,223 warnings, zero errors/no new ownership-source
warnings. Final default full solution matrix: exit 0, nine successful runs,
**5,178 passing executions**, zero failures/skips. Windows SDK 10.0.401; Forms 221/net8,
Setup 232/net9, Studio 66/net9, Migration 160/TFM, reliability 1,393/TFM on net8/9/10.
Each final test assembly executes all 54 ownership cases, including child processes.

Commands remain forced build then no-build/no-restore full solution test with
GeneratePackageOnBuild=false and GenerateDocumentationFile=false. Final logs:
`$env:TEMP/BeepDM-migration-ownership-qualified-rebuild.log` and
`$env:TEMP/BeepDM-migration-ownership-qualified-matrix.log`. Initial/second matrices
and lifecycle/pipes rechecks are retained under the migration-ownership log prefix.
This-run tracked Setup bin/obj changes restored; existing Engine/Models docs.xml
changes are preserved. All launched tool sessions are terminal.
Final validation: 13 framework/ownership documents, 196 local links and 96 in-range
source anchors pass; scoped diff whitespace checks pass. All 162 ownership executions
are present in the final matrix, not inferred from suite totals.

## Post-NFEL Planning Review - 2026-10-03

Planning progress only: reviewed current sync promotion/completion, migration
admission/progress, configuration facades, Forms timers, import/ETL source reads
and package projects. Refreshed REVIEW-AND-PLAN-2026-10-03.md plus current/master/
roadmap entry points. Historical NFEL defects are marked corrected. Next bounded
P1 slice is migration physical-target/store ownership and private progress; sync
publication/cursor agreement and remaining config facades are independent P1
lanes. Added claim/crash, publication-fault, facade and decision acceptance criteria.
No implementation, tests or skills edited; no phase checkbox completed.

Forced solution build with package/documentation generation disabled: exit 0,
6,223 warnings, zero errors. Full no-build/no-restore source-built Windows matrix:
exit 0, nine successful runs, 5,016 passes, zero failures/skips. SDK 10.0.401;
Forms 221/net8, Setup 232/net9, Studio 66/net9, Migration 160/TFM and reliability
1,339/TFM on net8/net9/net10. Logs:
`$env:TEMP/BeepDM-framework-review-current-build.log` and
`$env:TEMP/BeepDM-framework-review-current-tests.log`. This-run tracked Setup
bin/obj changes restored; pre-existing Engine/Models docs.xml changes preserved.
No new concurrency fault, live provider, Unix, clean pack, package consumer or
benchmark qualification. The full five-phase objective remains active.

## NFEL Grammar, Execution Admission And Owned Retention - 2026-10-03

Previous goal turn classification: progress. The planning review changed authoritative
delivery documents and reproduced five actual NFEL defects, selecting this bounded
implementation slice under R08/P3-09/P4-07/P5-05/07. The full five-phase objective
remains active; no broad checkbox is completed or scope reduced.

### Implemented

- Replaced regex match-subset admission with a complete bounded lexer/grammar and
  private AST, used by both NfelParser and actual NFEL RuleEngine execution.
  Unknown text, malformed operand/operator/nesting/escape/numeric input deny.
  Strings are decoded; subtraction, unary signs, right-associative power and
  advertised ternary work. Logical/ternary branches are lazy but all tokens pass
  captured policy. Other parser profiles retain their shared legacy evaluator.
- NFEL-1 retains Double arithmetic/literals with explicit finite binary64 semantics;
  integral/Decimal parameter comparisons remain exact. Conditions require Booleans;
  mixed invalid types, implicit numeric strings and arbitrary objects are not
  converted. Zero division, nonfinite values and returned-string bounds are typed
  faults. This is not arbitrary-precision/mixed-number financial qualification.
- Registration captures original expression spelling while lookup stays caseless.
  Each solve re-admits source and compares pre-populated/changed structure tokens;
  direct evaluation snapshots its tokens. Private trees and bounded policy copies
  survive callback mutation. Transport versions other than current 1.0 deny.
  Fresh Draft rules respect lifecycle minimums. Parameter lookup/capture failures,
  including observer-thrown RuleExceptions, do not echo their messages/payloads.
- Added hard source/token/node/depth/string/identifier/numeric limits. Successful
  history evicts oldest entries at 128 structures, 1,048,576 source characters or
  32,768 tokens. Failed parses are not retained. Returned parse/history structures
  and tokens are independent; Clear/concurrent parse is synchronized. Active ASTs
  are separate from history. Caller-held/transient allocations are not a global
  process-memory guarantee.
- Appended Power/Question/Colon token kinds without changing existing ordinals;
  public interfaces and Models transport schema remain unchanged. Added NFEL.md,
  README routing, canonical rules-engine skill/reference and direct Codex installed
  copies. The direct rules-engine folder was absent, so it was installed there;
  no nested/.agents/harness rules skill was fabricated. Canonical/installed skill
  metadata validates and normalized contents agree; line endings differ.
- Refreshed current/master/roadmap/phase evidence, the review follow-up and current
  agent test inventory. No unrelated worktree changes were reverted.

### Regression Evidence

The initial net9 run before runtime edits had 31 tests: 28 failed, three passed.
Log: `$env:TEMP/BeepDM-nfel-red.log`. The first implementation passed all 31.
The expanded audit had 88 cases: 85 passed/three failed. Two runtime defects were
fresh-rule lifecycle bypass and raw observer-thrown RuleException propagation;
both were fixed. The third failure was an incorrect new enum fixture expectation:
the untouched net8 Models DLL confirmed existing Unknown=24 and CloseParen=26,
and the fixture was corrected to those authoritative values, not the enum order.
Audit-fixed passed 88; the next net9 run passed 94. Five final schema/policy cases
bring the new suite to 99 cases per TFM. No failures were suppressed or removed.

The cases assert actual direct/SolveRule values, the five planning probe failures,
lazy and typed semantics, finite arithmetic, policy/lifecycle, source/token mutation,
safe failures, exact boundary/node/tree/retention behavior, synchronized history,
transport rejection and other-parser/ordinal compatibility. The custom IRule method
throws if invoked: expression execution, not a mocked custom-rule shortcut, is tested.

### Final Verification

```powershell
dotnet build BeepDM.sln --no-incremental -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q
dotnet test BeepDM.sln --no-build --no-restore -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Windows SDK 10.0.401. Both exit 0. Forced rebuild: 6,223 warnings, zero errors;
warning count is unchanged and no warning names the new NFEL runtime/tests.
Nine successful test runs: Forms 221/net8, Setup 232/net9, Studio 66/net9,
Migration 160 each on net8/9/10 and reliability 1,339 each on net8/9/10.
Total 5,016 passes, zero failures/skips. The log contains 297 passing NFEL case
executions (99 on each TFM), not only a net9 targeted run.
Logs: `$env:TEMP/BeepDM-nfel-final-rebuild.log` and
`$env:TEMP/BeepDM-nfel-final-matrix.log`.

Final validation: 15 framework/contract/skill documents, 201 existing local links
and 99 in-bounds source anchors. Corrected one obsolete historical NFEL line anchor
without rewriting its review evidence. Scoped diff checks and new-code ASCII/
trailing-whitespace checks pass. Both skill folders validate; canonical/installed
SKILL.md and reference.md match after line-ending normalization.

Tracked Setup bin/obj was clean at the start; this run's generated changes are
restored. Pre-existing Engine/Models docs.xml remains intact/dirty; docs generation
was disabled. Skill validation is metadata evidence, not behavioral qualification.

### Remaining Gates

- Other parser/helper dialects, broader numeric profiles, wrapper/host/package and
  persisted-reader compatibility remain under P3-09/P5. Old quoted/subtraction/
  unknown-ternary NFEL structures require explicit reviewed reparse. Old engines
  are not qualified readers for appended token kinds.
- Parameter/host context is not an immutable run snapshot. Registry concurrency,
  unknown-key/audit diagnostic behavior, observer isolation and executable plugin
  side effects remain separate. Timeouts cannot interrupt trusted blocking callbacks;
  these legacy APIs have no CancellationToken. NFEL is not a security sandbox.
- NFEL bounds do not complete P4 memory/workload/plugin/history requirements.
  No live providers, Unix, clean pack/package consumers or performance benchmarks
  were verified. P1 sync promotion/cursor agreement, migration claims/private
  progress and remaining configuration/security facades remain open.
- Keep the entire P1-P5 objective active and continue the independent P1 lanes and
  remaining intent/provider/release work, rather than treating this slice as completion.

## Framework Review And Enhancement Plan Refresh - 2026-10-03

Planning-only response to the user's framework review/plan request. The active
five-phase implementation objective is neither completed nor paused by this
planning pass. Existing runtime/test/skill changes and all phase checkboxes remain.

### Reviewed And Planned

- Added REVIEW-AND-PLAN-2026-10-03.md with eight prioritized findings, architecture
  direction, existing work IDs, delivery dependencies, responsibility roles and
  concrete merge gates. Refreshed current/master/roadmap entry points and P3-09;
  relabeled older planning baselines so they are not presented as current evidence.
- Preserved tested required-default/catalog/roster/expression/query/identity/date/
  configuration work. Remaining P1 gaps are sync promotion/cursor publication,
  migration exclusive admission/private progress and other configuration facades.
  Independent provider and reproducible-release inventory starts in parallel.
- Manual probes against freshly rebuilt net10 test-directory DLLs reproduce five
  NFEL defects: `1 + 2 @` parses and returns 3; `1-2`/ternary/reversed parentheses
  parse then throw; `name == 'Alice'` returns false for an actual Alice value.
  All five structures remain retained. These are public ParseRule/EvaluateExpression
  executions in PowerShell/.NET 10.0.12, not new automated test cases.
- The proposed bounded NFEL slice includes actual SolveRule execution, pre-parsed
  mutation admission, policy/lazy semantics and retention, not just parser errors.
  Source-derived concurrency/storage/lifecycle risks still need injected regressions.

### Fresh Verification

```powershell
dotnet build BeepDM.sln --no-incremental -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q
dotnet test BeepDM.sln --no-build --no-restore -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Windows SDK 10.0.401. Both exit 0. Build: 6,223 warnings, zero errors. Nine successful
test runs: Forms 221/net8, Setup 232/net9, Studio 66/net9, Migration 160 each on
net8/9/10 and FrameworkReliability 1,240 each on net8/9/10. Total 4,719 passes,
zero failures/skips. Logs: `$env:TEMP/BeepDM-framework-planning-refresh-build.log`
and `$env:TEMP/BeepDM-framework-planning-refresh-tests.log`.

Tracked Setup bin/obj was clean at the start and this run's generated changes
are restored. Pre-existing dirty Engine/Models docs.xml is preserved; documentation
generation was disabled. No runtime/test/skill source edits. Local green tests do
not establish NFEL correctness, live providers, Unix, clean isolated pack, package
consumers, complete credential coverage or performance budgets. Full P1-P5 scope
and remaining gates stay open; no phase item is newly completed.

## Required Configuration Namespaces And Host Maps - 2026-10-03

Previous goal turn classification: progress. The required date increment passed
4,470 executions. This increment continues B1/R15/P3-09/P5-05/07 without closing a
broad phase item or shrinking the full five-phase implementation objective.

### Implemented

- Exact shipped ConfigurationResolver required work routes all seven declared
  operators in function/colon/dot forms and validates one atomic bounded key before
  host-source callbacks. Quoted hierarchical/punctuation keys retain meaning.
  Direct exact-shipped calls inside required frames also validate grammar/limits
  and report sticky safe failure; legacy direct/custom subclass behavior is separate.
- Explicit named AppSettings/AppConfig/WebConfig/ConnectionStrings Objects entries
  provide flat IReadOnlyDictionary<string,string> sources. Duplicate namespaces
  deny before callbacks. Imported rows, arbitrary objects and editor configuration/
  credential backends are never inferred. Null/invalid/missing explicit values do
  not fall through to environment settings. Host-only AppConfig/WebConfig require
  actual sources rather than pretending to read config files.
- The complete selected map is captured into a private case-insensitive string
  dictionary before selection, without source indexer/TryGetValue/Keys/Values calls.
  Count/visited entries must agree and stay within 10,000; context Objects has the
  same entry cap. Keys have a 1,024-character/control/ambiguity bound, nonnull values
  retain genuine empty/whitespace meaning and the 1-MiB literal limit, with a
  16-MiB estimated aggregate budget. Invalid tails cannot acknowledge an earlier key.
- Explicit cursor control checks cancellation/sticky failure around Count,
  GetEnumerator, MoveNext and Current, and after disposal. Known failure prevents
  later business callbacks; disposal remains cleanup. Callback/iteration/disposal
  failure preserves acknowledged prefixes as Partial, without replay or uncertainty
  claims. Strings are immutable after capture; host coordination, blocking callback
  effects and run-wide context ownership remain separate qualifications.
- Without explicit AppSettings, only Process APPSETTING_<key> is used, not a bare
  variable fallback; ENV(key) expresses that intent. ConnectionStrings uses its two
  declared Process aliases, admitting one actual value or identical values and
  denying conflicts. Reads remain dynamic, not an atomic multi-variable snapshot.
  No credential decryption, catalog lookup, host keys or persistence behavior changes.
- Updated admission contracts, canonical/harness/direct/nested import/sync skills,
  plan baselines and host guidance. No public Models API change; .agents untouched.
  Explicit host maps are not automatically injected into ordinary import contexts.
  Per-resolution capture is not an immutable run configuration or host bridge.

### Verification

The precise initial **41-case** net9 configuration inventory gave **24 failures/
17 passes**, exit 1, before runtime edits, in
`$env:TEMP/BeepDM-required-configuration-red.log`. No fixture assertions were removed
or required dispatch disabled for this evidence. Initial combined audit passed
521 cases; callback-boundary audit passed 552. Final new inventory is **83 cases per
TFM**; the combined required resolver audit passes **563**, exit 0, in
`$env:TEMP/BeepDM-required-configuration-audit-final.log`.

Coverage includes declared namespaces/aliases, exact punctuation/case, genuine empty
values, missing/invalid/ambiguous host sources, whole-source key/type/count/size/
budget limits, null/lying enumerators, observer nonconversion, callback/disposal
exceptions and cancellation, sticky reported failure, no post-cancellation business
reads, no editor/row inference, dynamic environment changes, captured value retention
after source disposal mutation, partial counts and legacy/direct/subclass behavior.

Forced source-built Windows matrix, SDK **10.0.401**:

```powershell
dotnet build BeepDM.sln --no-incremental -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q
dotnet test BeepDM.sln --no-build --no-restore -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Both commands exit 0. IL inspection confirms required configuration dispatch in
each test-directory Engine assembly. **4,719 passing executions**, nine successful
runs, zero failures/skips: Forms 221, Setup 232, Studio 66, Migration 160 on each
net8/net9/net10 and Reliability 1,240 on each net8/net9/net10 (3,720 reliability
executions). Build log: `$env:TEMP/BeepDM-required-configuration-rebuild.log`; matrix:
`$env:TEMP/BeepDM-required-configuration-final.log`. Forced rebuild has 6,223 existing
warnings and zero errors; no warnings reference the new configuration helper/tests.

Eight skill folders validate; eight installed SKILL/reference files match canonical
SHA256 hashes. Fifteen plan/contract documents have 177 existing local links and
84 in-bounds source anchors (existence/bounds, not semantic proof). Both new C# files
pass ASCII/trailing-whitespace checks. Generated tracked Setup bin/obj artifacts
from this turn were restored; pre-existing dirty docs.xml remains untouched.

### Remaining Gates

NFEL/profile semantics, explicit host configuration bridging and immutable run
policy/plugin/context/identity/time, source writer coordination, all-route secret
redaction/persisted admission compatibility and credential-reader qualification,
provider/query isolation/translation and native/provider recovery, B2 promotion/
cursor agreement, configuration persistence/security, migration/lifecycle, P4
performance/telemetry and P5 package/CI/release consumers remain open. This is
progress, not R15 or phase closure.

## Required Date Admission And Typed Composition - 2026-10-03

Previous goal turn classification: progress. The identity/scope increment passed
4,218 executions. This increment continues B1/R15/P3-09/P5-05/07 without closing a
broad phase item or shrinking the full five-phase implementation objective.

### Implemented

- Exact shipped DateTimeResolver required work parses bounded known date trees
  before nested callbacks. Gregorian ISO literals replace culture-dependent
  interpretation: no zone yields Unspecified DateTime, Z yields UTC DateTime and
  signed HH:mm offsets yield DateTimeOffset without host-zone conversion. Invalid
  or ambiguous literals do not become current-clock substitutes.
- Days/hours/minutes use signed invariant decimal text and exact bounded BigInteger
  numerator/denominator division to whole 100-nanosecond ticks. No floating/decimal
  rounding or sub-tick truncation is admitted. Months/years require Int32 integers;
  checked result ranges deny overflow and retain actual calendar clamping. Input
  is bounded to 28 digits/64 characters. Kind/offset survives arithmetic.
- FORMAT/DATEFORMAT use invariant formatting, with 1,024 format characters and
  16,384 result characters. Known nested offsets/formats and bare-token arity are
  validated before callbacks. Malformed formats do not return unrelated date text.
  Rule/date-plan limits remain 16,384 characters/32 levels; registry depth applies.
- Nested date tokens/functions use the pinned registry and actual IPassedArgs,
  including actual row properties and custom overrides. Only actual DateTime or
  DateTimeOffset is a date base; strings, TimeSpan, DateOnly and observer objects
  are not reparsed/coerced. Failure/cancellation stays sticky; acknowledged prefixes
  remain Partial without replay. Bare clock evaluation uses one UTC instant for
  local calendar/time derivation and explicit UTC/Local kinds. Clock leaves stay
  dynamic, not immutable run time or named-zone/DST scheduling qualification.
- Final source audit closed a direct exact-shipped empty-rule early return that
  could fabricate a clock in a required frame. It now reports sticky safe failure.
  Legacy direct behavior outside frames and custom subclass contracts stay separate.
  Updated contracts, canonical/harness/direct/nested import/sync skills, plan
  baselines and host guidance. No public Models API change; .agents untouched.

### Verification And Artifact Qualification

The first fixture compile used a nonexistent three-argument registration overload;
corrected it to the existing resolver Priority contract. The first filtered run
included three older cases. Subsequent fixture audit corrected SentData versus
actual ReturnData and prevented C# conditional DateTime-to-DateTimeOffset implicit
conversion before boxing. Inputs now assert their exact types; no assertions were
removed. With the new dispatch temporarily disabled, the corrected precise initial
**55-case** inventory gave **22 failures/33 passes**, exit 1, in
`$env:TEMP/BeepDM-required-date-qualified-red.log`. Required dispatch was restored
before green verification. The initial expanded audit passed 477 cases.

The first incremental full matrix passed net8/net9 but failed 31 net10 cases.
IL inspection of the actual test-directory Engine assemblies showed net10 retained
the disabled date dispatch from red testing; net8/net9 contained the required call.
This is failed artifact qualification, not green source evidence. A forced solution
rebuild produced all three required calls and a verified intermediate **4,461-pass**
matrix with 81 new date cases per TFM. Logs retain the failed incremental run and
the successful intermediate rebuilt run rather than replacing their history.

The final direct-empty-rule audit added three cases, all failing on the old early
return, exit 1: `$env:TEMP/BeepDM-required-date-direct-red.log`. After the fix a second
forced rebuild and full matrix passed. Final new inventory: **84 cases per TFM**.
Focused final combined required resolver suite: **480 passes**, exit 0, in
`$env:TEMP/BeepDM-required-date-audit-final.log`.

Final source-built Windows matrix, SDK **10.0.401**:

```powershell
dotnet build BeepDM.sln --no-incremental -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q
dotnet test BeepDM.sln --no-build --no-restore -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Both commands exit 0. **4,470 passing executions**, nine successful runs, zero
failures/skips: Forms 221, Setup 232, Studio 66, Migration 160 on each net8/net9/net10
and Reliability 1,157 on each net8/net9/net10 (3,471 reliability executions).
Build log: `$env:TEMP/BeepDM-required-date-rebuild-final.log`; matrix log:
`$env:TEMP/BeepDM-required-date-final-complete.log`. Forced rebuild has 6,223 legacy
warnings and zero errors; no warnings reference the new date helper/test files.
This is local source-built verification, not clean-pack/all-platform qualification.

Eight skill folders pass quick_validate; eight installed SKILL/reference files
match canonical SHA256 hashes. Fifteen plan/contract documents have 176 existing
local links and 84 in-bounds source anchors (existence/bounds, not semantic proof).
Both new C# files pass ASCII/trailing-whitespace checks. Generated tracked Setup
bin/obj changes from this turn were restored; existing dirty docs.xml preserved.

### Remaining Gates

NFEL/config/profile semantics, complete immutable policy/plugin/context and owned
run identity/time, named-zone/host-clock/all-platform qualification, all-route
diagnostics and persisted admission compatibility, provider/query translation and
isolation, native/provider recovery, B2 promotion/cursor agreement, configuration
security, migration/lifecycle, P4 performance/telemetry and P5 package/CI/release
consumers remain open. This is progress, not R15 or phase closure.

## Required Identity And Environment Scope - 2026-10-03

Previous goal turn classification: progress. The query-admission increment passed
4,050 executions. This increment continues B1/R15/P3-09/P5-05/07; the full
five-phase objective and broader qualification gates remain open.

### Implemented

- Exact shipped UserContextResolver required work accepts explicit, bounded string
  identity context only. Missing email/role does not fabricate a value; an
  application-specific role does not fall back to a generic role. Ambiguous
  property/named-entry sources deny before property callbacks. ReturnData and
  arbitrary Objects records are not identity sources. Supplied host strings are
  data, not authentication or authorization proof.
- Account names, Windows SID/principal and verified builtin group membership use
  actual OS APIs. Windows identity handles are disposed; no matching builtin group
  denies rather than returning a placeholder. These groups are not primary groups
  or application roles. Required cancellation/sticky failure surrounds callbacks.
- Profile defaults use actual SpecialFolder values; TEMP uses Path.GetTempPath.
  Downloads uses the Windows shell known-folder API with balanced owned COM
  initialization and task-memory cleanup, including failure paths. Existing STA
  apartment ownership is preserved. Unknown/unavailable folders deny instead of
  substituting the profile root. No host profile or registry configuration changes.
- Exact shipped EnvironmentResolver required work preserves requested Process,
  User or Machine scope without cross-scope fallback. SYSTEMPATH is Machine PATH,
  USERPATH is User PATH, and ENV(PATH) selects inherited Process PATH. Colon aliases
  retain quoted variable-name punctuation. Unsupported persisted scope on Unix
  denies explicitly. Reads remain dynamic, not immutable run-context capture.
- Legacy direct calls and custom subclass semantics remain separate. Updated
  admission contracts, canonical/harness/direct/nested import and sync skills,
  plan baselines and host guidance. No public Models API changes; .agents untouched.

### Verification

The initial 44-case run included eight prior cases and fixture defects in positive
payload capture. Corrected fixtures inspect actual InsertEntity payloads and clear
only pre-run registration invocations; no assertions were removed. With just the
new required dispatches temporarily disabled, the precise 36-case inventory gave
**24 failures/12 passes**, exit 1, in
`$env:TEMP/BeepDM-required-identity-scope-qualified-red.log`. Both required dispatches
were restored before green verification and the subsequent audit.

Final new inventory: **56 cases per TFM** in ImportRequiredIdentityScopeTests.
Coverage includes explicit/ambiguous/malformed context, observer and getter
boundaries, cancellation, ReturnData spoof rejection, exact application-role keys,
environment scopes and punctuation, actual account/folder values, STA/MTA Downloads
apartment preservation, builtin membership, legacy behavior and custom subclasses.
Focused combined required-resolver audit: **396 passes**, exit 0, in
`$env:TEMP/BeepDM-required-identity-scope-audit.log`.

Full source-built Windows solution matrix with SDK **10.0.401**:

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

**4,218 passing executions**, nine successful runs, zero failures/skips, exit 0:
Forms 221, Setup 232, Studio 66, Migration 160 on each net8/net9/net10 and
Reliability 1,073 on each net8/net9/net10 (3,219 reliability executions).
Log: `$env:TEMP/BeepDM-required-identity-scope-final.log`. No warnings reference the
two new required helper files in this run; existing legacy diagnostics remain.
Only generated Setup bin/obj artifacts from this run were restored.

Eight skill folders pass quick_validate; eight installed SKILL/reference files
match canonical SHA256 hashes. Fifteen plan/contract documents have 174 existing
local links and 84 in-bounds source anchors; this checks existence/bounds, not
semantic anchor accuracy. The three new C# files pass ASCII/trailing-whitespace
checks. Native failure injection and redirected-folder mutation are not exercised.

### Remaining Gates

Date/NFEL/config/profile semantics, owned host identity/authentication policy and
immutable context capture, all-platform/native failure qualification, complete
provider/plugin intent and diagnostic routes, query isolation, B2 promotion/cursor
work, configuration security, migration/lifecycle, P4 performance/telemetry and P5
package/CI/release consumers remain open. This is progress, not R15 or phase closure.

## Required Datasource Query Admission And Typed Aggregates - 2026-10-03

Previous goal turn classification: progress. The strict-expression increment
passed 3,810 executions. This increment continues B1/R15/P3-09/P5-05/07 without
closing a broad item or shrinking the full five-phase implementation objective.

### Implemented

- Exact shipped DataSourceResolver required work builds a closed query plan before
  context/provider callbacks. All modes, entity/field roles and trailing filters
  are validated; malformed filters cannot be dropped to broaden a read. Quoted
  names/values retain punctuation, operator scanning respects quotes and only
  unquoted @Name binds. Missing/ambiguous/null/unsupported context cannot become
  literal substitute text. Binding uses named entries/explicit FieldName/actual
  ReturnData or an explicit named Record, with closed invariant scalar formatting.
- Null collections deny, while actual empty COUNT/EXISTS are 0/false. Required
  scalar results use closed bounded literal admission and clone bytes. FIRST
  deliberately returns the actual record, without deep-ownership claims. Empty
  required values do not fabricate aggregate/lookup defaults.
- Streaming consumption replaces ToList, with 100,000 visited rows, 256 filters,
  260 arguments, 10,000 named entries, 1-MiB bound values and a 16-MiB estimated
  filter-text budget. SUM/AVG reuse required Decimal/IEEE arithmetic and finite
  Double output; MAX/MIN preserve selected typed scalar/date values and exact
  ordering. Aggregates skip only real null fields, never malformed/missing values.
- Cancellation/sticky failure and root provider status are checked around cursor/
  field callbacks and after disposal. Reported non-Ok/exception results deny even
  a nonnull collection; known denial prevents later diagnostic getters. Callback/
  enumeration/disposal failure preserves earlier destination acknowledgements
  as Partial without replay. FIRST/SCALAR/EXISTS inspect only the first row; the
  unused tail is not prequalified.
- Explicit query handles/names retain host ownership; ordinary imports do not
  implicitly choose one. Nullable/shared root ErrorObject is not read acknowledgement,
  isolation or hidden-error proof. GetEntity can block/eagerly allocate, and no
  callback effects are rolled back. Legacy direct/custom query semantic contracts
  remain separate. Updated canonical/harness/direct/nested import/sync skills,
  contracts, current plan baselines and host guidance; .agents stays untouched.
  No public Models API/signature change or broad phase checkbox completion.

### Verification

Initial 46-case net9 run: 37 failures/nine passes before runtime edits, but fixture
audit found that Moq's default IEnumerable was empty, not null, and logging checks
included pre-run plugin registration. Null collections are now explicitly configured
and only pre-run registration invocations are cleared; no assertions were removed.
Re-ran those corrected 46 cases with the legacy query dispatch temporarily restored:
**36 failures/10 passes**, exit 1: `$env:TEMP/BeepDM-required-query-qualified-red.log`.
The required dispatch was reinstated before subsequent verification.

Final new inventory: **80 cases per TFM** in ImportRequiredQueryTests. Real payloads/
filters, culture, typed exact 64-bit/date/Decimal values, null/empty distinction,
malformed/missing values and overflow, binary/DataRow ownership, consumption/filter
limits, cursor/disposal/root-status failure, both cancellation boundaries, nested
expressions/custom overrides, legacy behavior and Partial counts are asserted.
The bounded-consumption fixture clears accumulated mock call history periodically
so the mock itself does not create an unrelated unbounded memory test. Combined
net9 import resolver suite: **340 passed**, exit 0:
`$env:TEMP/BeepDM-required-query-audit-final.log`.

Final source-built Windows SDK 10.0.401 matrix: **4,050 passed**, nine successful
runs, zero failures/skips. Forms 221, Setup 232, Studio 66; Migration 160 and
FrameworkReliability **1,017 each** on net8/net9/net10. Command:
`dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'`.
Log: `$env:TEMP/BeepDM-required-query-final.log`; terminal exit 0. Existing compiler/
analyzer and legacy initialization diagnostics remain. Restored only this run's
tracked generated Setup bin/obj; pre-existing source/docs.xml changes are preserved.

Eight canonical/harness/direct/nested skill folders pass quick_validate; eight
installed SKILL/reference hashes match canonical. Fifteen plan/contract documents
have 174 existing local links and 84 source-line anchors within bounds. Scoped
git diff --check and explicit ASCII/trailing-whitespace checks on both new code
files pass. These metadata checks do not qualify semantics or live providers.

### Remaining Scope

Next qualify required user identity/role and environment cross-scope fallbacks,
date/numeric/profile semantics, NFEL and all-route diagnostics. Query context/
handle ownership, hidden/nested provider errors, allocation/isolation/translation
and live fixtures remain open, not inferred from mock read results. Complete
policy/plugin/provider intent, native/live-provider recovery and package consumers
remain. B2 promotion/cursor agreement, config/security and migration admission,
lifecycle/drain, performance, SDK/CI/pack/release and the full P1-P5 scope stay
active. No completion or blocker is claimed.

## Strict Required Expression And Formula AST - 2026-10-03

Previous goal turn classification: progress. The dot-literal increment passed
3,564 executions. This increment continues B1/R15/P3-09/P5-05/07 without closing
a broad item or reducing the full five-phase implementation objective.

### Implemented

- Exact shipped ExpressionResolver/FormulaResolver required paths parse a bounded
  AST before field getters/nested plugin callbacks. Full syntax consumption,
  operator precedence, Boolean-only conditions and actual flat field reads replace
  substring routing, truthy strings and implicit observer conversion. Token/node/
  AST/parser bounds are 4096/1024/32/32 within existing length/envelope admission.
- Typed comparisons preserve integral/Decimal distinctions, including adjacent
  64-bit values above Double's exact integer range. Same-type floating equality
  has no epsilon; mixed numeric comparisons require exact admitted Decimal
  roundtrip representation. Strings remain ordinal-ignore-case, not numeric
  coercions. Null/Boolean/Guid comparison roles and incompatible types fail closed.
- Numeric literals/math-call quoted operands use invariant decimal/scientific
  notation with exact Decimal/Double roundtrip admission. Lossy literals,
  overflow/underflow/non-finite results and division/remainder by zero deny writes.
  Decimal arithmetic retains .NET bounded scale; floating arithmetic uses finite
  Double. Computed/literal numeric results retain the established Double boundary,
  not arbitrary precision. Decimal midpoint ROUND is AwayFromZero; MATH ROUND
  remains ToEven. Quoted MATH names and inclusive Int32 RANDOM endpoints work.
- IF/CASE/null coalescing/logical operations are lazy. Unused branches are parsed
  for syntax, not semantically prequalified/evaluated. Selected custom nested
  calls retain captured roster selection, cancellation/sticky failure and their
  own semantic contracts. Getters/plugins remain synchronous trusted code, not a
  sandbox or rollback boundary. Legacy direct/custom resolver paths stay unchanged.
- Updated defaults admission, host guidance, current review/roadmap/trackers and
  canonical/harness/direct/nested Codex import/sync skills. .agents is untouched;
  no public Models API/signature changes or broad phase checkbox completion.

### Verification

Initial 48-case net9 regression run: **39 failures/nine passes**, exit 1, before
runtime changes: `$env:TEMP/BeepDM-strict-expression-red.log`. No failed assertions
were removed. Final inventory: **82 cases per TFM** in ImportStrictExpressionTests.
Actual payloads, typed/culture/precision cases, midpoint rounding, max random
endpoints, lazy getters, whole-tree pre-read limits, cancellation after a getter,
nested custom math selection and legacy compatibility are asserted. The combined
net9 import resolver suite passes **260**, exit 0:
`$env:TEMP/BeepDM-strict-expression-audit.log`.

Final source-built Windows SDK 10.0.401 matrix: **3,810 passed**, nine successful
runs, zero failures/skips. Forms 221, Setup 232, Studio 66; Migration 160 and
FrameworkReliability **937 each** on net8/net9/net10. Command:
`dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'`.
Log: `$env:TEMP/BeepDM-strict-expression-final.log`; terminal exit 0. Existing
compiler/analyzer and legacy initialization diagnostics remain. Restored only
this run's tracked generated Setup bin/obj; preserved pre-existing source/docs.xml.

Eight canonical/harness/direct/nested skill folders pass quick_validate; eight
installed SKILL/reference files match canonical SHA256. Fifteen plan/contract
documents have 174 existing local links and 84 source-line anchors within bounds.
Scoped git diff --check and explicit ASCII/trailing-whitespace checks on both new
code files pass. These are metadata checks, not extra semantic/provider proof.

### Remaining Scope

Next qualify required datasource filter/aggregate failures, user identity/role and
environment cross-scope fallbacks, date/numeric/profile/provider semantics, NFEL
and all-route diagnostics. Unused branch semantic admission, arbitrary plugin
dialects and instance immutability are not inferred from a shipped AST. Complete
policy/plugin/provider intent, native/live-provider recovery and package consumers
remain open. B2 promotion/cursor agreement, parallel config/security and migration
admission, lifecycle/drain, performance, SDK/CI/pack/release and the full P1-P5
scope stay active. No completion or blocker is claimed.

## Required Dot Normalization And Literal Preservation - 2026-10-03

Previous goal turn classification: progress. The shipped outer-grammar increment
passed 3,423 executions. This increment continues B1/R15/P3-09/P5-05/07 without
closing a broad item or reducing the full five-phase implementation objective.

### Implemented

- Required resolution has an internal dot-style parser/normalization path. Public
  legacy parsing is unchanged. Required missing/trailing segments, malformed quote
  fragments and unquoted top-level commas cannot be erased before admission.
  Legacy bare-token normalization retains error diagnostics instead of replacing
  them with a missing-prefix warning. Unknown custom dot dialects pass through.
- Required literals retain quotes, explicit empty strings and punctuation; quoted
  string/Boolean/null/field-like operands are not silently reinterpreted as fields
  or other scalar types. Quoted EXPRESSION/EVAL literals bypass operator substring
  routing. Existing explicit function-style literals benefit from the same check.
- Dots outside quotes/nested calls separate arguments deterministically. Quote
  decimal arguments; shipped numeric helper conversion unwraps quoted operands in
  required work. Short math aliases and other shipped tokens participate in the
  required dialect. This does not qualify all numeric/culture/semantic behavior.
- Grouping quotes apply only to declared condition/calculation/date/logical/math-
  name/query mode/filter positions. Nested date calls, decimal conditions and
  actual provider filter punctuation retain meaning without top-level comma
  injection. Known-operator overrides receive canonical quotes/roles; arbitrary
  plugins remain trusted and semantically unqualified, not sandboxed.
- Updated the defaults contract, canonical/harness/direct/nested Codex import/sync
  skills, plan baselines and host guidance. .agents copies are untouched. No public
  Models API/signature changes and no broad phase checkbox completion.

### Verification

Initial 41-case net9 regression run: **31 failures/10 passes**, exit 1, before
runtime changes: `$env:TEMP/BeepDM-required-dot-red.log`. No failed assertions were
removed. New inventory: **47 cases per TFM** in ImportRequiredDotRuleTests.
Actual destination payloads, nested lookup filter arguments, malformed-rule
zero-write/pre-read boundaries, custom overrides/unknown dialects and public
legacy compatibility are asserted. The combined net9 import resolver suite passes
**178 cases**, exit 0: `$env:TEMP/BeepDM-required-dot-green.log`.

Final source-built Windows SDK 10.0.401 matrix: **3,564 passed**, nine successful
runs, zero failures/skips. Forms 221, Setup 232, Studio 66; Migration 160 and
FrameworkReliability **855 each** on net8/net9/net10. Command:
`dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'`.
Log: `$env:TEMP/BeepDM-required-dot-final.log`; terminal exit 0. Compiler/analyzer
and legacy initialization diagnostics remain. Restored only this run's tracked
generated Setup bin/obj, preserving pre-existing source/docs.xml changes.

Eight canonical/harness/direct/nested skill folders pass quick_validate; eight
installed SKILL/reference files match canonical SHA256 hashes. Fifteen plan/contract
documents have 174 existing local links and 84 source-line anchors within bounds.
Scoped git diff --check and explicit ASCII/trailing-whitespace checks on both new
code files pass. These are metadata checks, not additional semantic/provider proof.

### Remaining Scope

Next qualify strict required expression truthiness/precedence/typed comparisons,
nested logical evaluation and complete numeric/culture behavior, datasource
filter/aggregate failure, identity/role and cross-scope fallbacks, NFEL and all-route
diagnostics. This slice is not complete immutable policy/plugin/provider intent,
live-provider conformance, native/provider-run recovery or package-consumer proof.
Remaining B2 promotion/cursor agreement, parallel config/security and migration
admission, lifecycle/drain, performance, SDK/CI/pack/release and the entire P1-P5
scope stay active. No completion or blocker is claimed.

## Required Built-In Outer Grammar And Routing - 2026-10-03

Previous goal turn classification: progress. Resolver/context ownership passed
3,246 executions; this increment continues B1/R15/P3-09/P5-05/07 without closing
a phase or reducing the full five-phase implementation scope.

### Implemented

- Required date/GUID/user/system routing matches exact operator tokens, not words
  inside arguments/keys. Unknown substring tokens cannot return host/generated
  substitutes; PROPERTY and CONFIG reach their intended resolvers.
- Nine exact shipped resolver types validate outer form/arity/atomic keys before
  execution. Empty comma slots, ignored extra arguments, adjacent quoted literals
  and invalid environment scope names deny required writes with safe outcomes.
  Datasource-query semantic validation remains separate. Custom types/subclasses
  retain their semantic contracts; shared base argument helpers use required
  structural splitting when called in that frame.
- The required splitter respects nested parentheses and quoted commas. Nested
  date offsets/formats work; quoted comparison operands and quoted colon keys
  retain supported meanings. GUID formats and NEWGUID()/GUID()/UUID()/
  GENERATEUNIQUEID() remain compatible. An existing NEWGUID() fixture exposed an
  initially over-restrictive gate; the gate was corrected, not the assertion.
- Required shipped SEQUENCE/INCREMENT/AUTOINCREMENT calls deny time/hash demo
  placeholders. A separately qualified allocator plugin is required for durable
  allocation. No uniqueness, monotonic sequence or provider allocation is invented.
  Legacy fallback/demonstration behavior remains outside required frames.
- Updated contracts, canonical/harness and direct/nested Codex import/sync skills,
  roadmap/current baselines and host guidance. .agents copies remain untouched.
  No public Models API/signature changes or broad phase checkbox completion.

### Verification

Initial net9 test compilation needed the actual Errors namespace (ConfigUtil).
After that fixture-only repair and before runtime changes, the 42-case regression
run had **38 failures/four passes**, exit 1:
`$env:TEMP/BeepDM-built-in-grammar-red.log`. New inventory is **59 cases per TFM**
in ImportBuiltInGrammarTests. Actual provider arguments cover denied writes and
positive row/config routing, GUID formats, nested dates, scopes and compatibility.
The net9 focused import resolver suite passes **131 cases**, exit 0:
`$env:TEMP/BeepDM-built-in-grammar-green.log`.

Final source-built Windows SDK 10.0.401 solution matrix: **3,423 passed**, nine
successful runs, zero failures/skips. Forms 221, Setup 232, Studio 66; Migration
160 and FrameworkReliability **808 each** on net8/net9/net10. Command:
`dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'`.
Log: `$env:TEMP/BeepDM-built-in-grammar-final.log`; terminal process exit 0.
Compiler/analyzer and legacy initialization diagnostics remain. Restored only this
run's generated tracked Setup bin/obj; retained pre-existing source/docs.xml changes.

Eight skill folders pass quick_validate; eight installed SKILL/reference files
match canonical SHA256 hashes. Fifteen plan/contract documents have 174 existing
local links and 84 source-line anchors within bounds. Scoped git diff --check and
explicit ASCII/trailing-whitespace checks on both new code files pass. These are
metadata checks, not extra semantic/provider qualification.

### Remaining Scope

This is outer grammar/routing qualification, not complete built-in/custom semantics.
Next qualify required dot-style token/literal preservation and strict expression
truthiness/precedence/nested evaluation, datasource filters/aggregate failure,
identity/role and cross-scope fallbacks, NFEL and all-route diagnostics. Dynamic
configuration/environment/provider values and mutable plugin internals remain
outside whole-run immutable intent. Then remaining B2 promotion/cursor agreement,
parallel config/security and migration admission, provider/native recovery,
lifecycle/drain, performance and clean SDK/CI/pack/package-consumer/release gates.
The implementation goal remains active and all five phases remain in scope.

## Admitted Resolver Rosters And Nested Required Context - 2026-10-03

Previous goal turn classification: progress. The run-owned defaults/capture
increment passed 3,117 executions. This increment continues B1/R15/P3-09/P5-05/07;
all five phases remain in scope and the implementation goal is still active.

### Implemented

- Required defaults admission pins the editor's ordered resolver instances and
  registered priorities before source reads. Declared sync directions are pinned
  before provider-opening preflight; reverse shares a forward rule-bearing roster.
  Replacement/removal/new priority cannot change admitted rows/directions. Fresh
  top-level runs/batches capture updates. Resolver callbacks remain outside locks.
- Async-flow operation context and synchronous required frames retain roster/editor
  binding through nested calls and restore prior state on success/failure/cancel.
  Concurrent runs on the same editor retain distinct rosters. Cross-editor nested
  rebinding is denied and cannot be hidden by catching the exception.
- Per-row enabled definitions and captured assignment names isolate resolver
  SentData edits from retained defaults and subsequent rows. Required named/default
  column lookup consumes admitted definitions/datasource rather than rereading
  mutable catalogs. Missing/foreign/undeclared lookups deny; recursion respects
  depth 32. The qualified entity.column key must be declared for the column wrapper.
- Nested facade, manager argument overloads, normalization/telemetry and default
  wrappers inherit required cache-bypass/failure semantics. Null/unknown/exception/
  reported child failure is sticky even if caught or returned as telemetry.
  Scoped telemetry omits raw diagnostic metadata and uses a safe generic error.
  Required default wrappers avoid legacy raw-rule catch logging and late lookup.
- Required editor registry bootstrap installs ten built-ins without registration
  observers or partial-success fallback. Explicit registration/public construction
  retain legacy observer behavior. Selector cancellation/reported failure stops
  further plugin callbacks rather than merely denying the eventual provider write.
- Updated contracts, canonical/harness/direct/nested Codex import/sync skills,
  plans and host guidance. No Models API or public signature changes; .agents
  duplicate skills are untouched. No broad phase checkbox is completed.

### Verification

Initial net9 regression run: **16 failures**, zero passes, exit 1:
`$env:TEMP/BeepDM-resolver-ownership-red.log`. Fixtures first corrected the dictionary
overload to the actual concrete API and retained the actual editor's manager under
the legacy initialization lock instead of racing its process-global accessor.
Later red runs reproduced three raw-rule wrapper logs (12 passed/3 failed) and two
post-denial selector callbacks (0 passed/2 failed):
`$env:TEMP/BeepDM-resolver-nested-wrapper-red.log` and
`$env:TEMP/BeepDM-resolver-selector-red.log`. No failure assertion was suppressed.
The GUID fixture now parses the documented string result instead of asserting a
Guid object; telemetry code was corrected against the actual result property names.

New inventory: **43 cases per TFM**, 40 ImportResolverOwnershipTests and three
SyncResolverOwnershipTests. Actual provider payloads cover source/read/write
registration edits, fresh-run refresh, deliberately seeded legacy cache, nested
wrapper failures/logging, foreign editors, per-row SentData, standalone mutation,
failure/cancellation restoration, concurrent same-editor runs, named/qualified
lookup/recursion, quiet bootstrap and selector callback boundaries.
The focused run before the final qualified-column/telemetry assertions passed 74
cases (42 new + 32 prior resolver cases), exit 0:
`$env:TEMP/BeepDM-resolver-ownership-qualified.log`.

Final source-built Windows SDK 10.0.401 matrix: **3,246 passed**, nine successful
runs, zero failures/skips. Forms 221, Setup 232, Studio 66; Migration 160 and
FrameworkReliability **749 each** on net8/net9/net10. Command:
`dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'`.
Log: `$env:TEMP/BeepDM-resolver-ownership-final.log`. Compiler/analyzer and legacy
initialization diagnostics remain. Only this run's tracked generated Setup bin/obj
are restored; pre-existing source/docs.xml changes remain intact.

Documentation/skill checks: eight canonical/harness/direct/nested skill folders
pass quick_validate; eight installed SKILL/reference files match canonical SHA256
hashes. Fifteen contract/plan documents have 174 existing local links and 84
source-line anchors within bounds. Scoped git diff --check and explicit ASCII/
trailing-whitespace checks for three new code files pass. These are metadata
checks, not additional runtime/provider qualification.

### Remaining Scope

R15 is not closed. Roster/definition ownership does not snapshot plugin internals,
dynamic configuration/environment/provider values or all policy/integration/metadata
graphs. Captured callbacks remain trusted code and can have side effects; no sandbox
or universal plugin/provider thread safety is claimed. Per-row defensive copies add
work and need representative performance qualification, not a guessed cache bypass.

Next qualify full built-in/custom rule grammar and semantics, remaining helper/
profile logging and plugin/policy context, persisted evidence and package consumers.
Then remaining B2 staged promotion/cursor agreement and wider P1-P5 provider/native
recovery, config/security, migration execution ownership, lifecycle/drain, streaming/
performance and reproducible release gates. No full immutable intent, row rollback,
native cancellation, all-route secret audit or broad completion is inferred.

## Run-Owned Defaults And Both-Direction Catalog Admission - 2026-10-03

Previous goal turn classification: progress. Required editor-owned resolution
passed 2,997 executions. This increment continues B1/R15 under P3-09/P5-05/07;
the complete five-phase implementation goal remains active, not complete.

### Implemented

- Replaced Prepare's caller-config mutation with internal editor/name-bound
  admission and a private execution configuration. Ordinary runs refresh implicit
  catalogs; explicit lists and genuine empty catalogs stay owned. Top-level
  settings, list containers and staging options are copied without populating
  caller defaults/provider metadata. No public validated flag or interface change.
- Closed bounded literals reject arbitrary structs/objects/enums and non-finite
  numbers. Byte arrays are copied at admission/binding and per row. Nonblank rules
  omit unused PropertyValue from captured SentData. Documented all supported types,
  per-literal/aggregate bounds and compatibility/coordination limits.
- Sync translates both declared configurations and captures both catalogs before
  provider-opening ValidateSyncOperation, schema preflight or Running publication.
  Reverse catalog denial produces safe typed TransformationAdmissionFailed with
  zero fabricated rows/writes; no forward work is admitted. Both directions retain
  initial definitions, including empty catalogs. Retries reuse captured defaults
  and reset forward filter containers. Later direction edits cannot change whether
  reverse execution was admitted. This does not pre-evaluate reverse row rules.
- Updated defaults/import/sync contracts, source/harness skills and directly
  installed root/nested Codex import/sync skills. Agent duplicate roots untouched.
  No broad phase checkbox is completed.

### Verification

Initial focused net9 run: nine prior catalog cases passed and all 14 new cases
failed (after correcting the Moq/DefaultValue type alias in fixtures):
`$env:TEMP/BeepDM-default-capture-red.log`. A subsequent run exposed provider opens
in early validation; capture was moved ahead of it rather than weakening assertions.
The later configuration-variable cleanup briefly failed compilation through a
reverse-variable shadow; corrected before final matrix verification.

Added **40 cases per TFM**: 33 ImportDefaultCaptureTests plus seven
SyncDefaultCaptureTests. Coverage includes reused config/catalog refresh, source-
read/enumerator edits, byte isolation, 21 closed literal types, opt-out, invalid/
non-finite/oversized and aggregate bounds, pre-open reverse denial, empty/nonempty
catalog retention and direction edits. Full net9 reliability: 706 passed, exit 0;
`$env:TEMP/BeepDM-default-capture-net9.log`.

Final source-built Windows SDK 10.0.401 matrix: **3,117 passed**, nine successful
project/TFM runs, zero failures/skips. Forms 221, Setup 232, Studio 66; Migration
160 and FrameworkReliability **706 each** on net8/net9/net10. Command:
`dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'`.
Log: `$env:TEMP/BeepDM-default-capture-final.log`. Existing compiler/analyzer and
legacy initialization diagnostics remain; this is not a clean-package or all-route
logging result. Only this run's tracked generated Setup bin/obj are restored;
pre-existing source/docs.xml changes are preserved.

Eight source/harness/direct/nested skill folders pass quick_validate; all installed
import/sync SKILL.md/reference.md hashes match canonical files. Checked 174 local
links and 84 line bounds in 15 plan/contract documents. Scoped diff whitespace and
new-code ASCII/whitespace checks pass. These checks are not behavioral qualification
of custom plugins or proof that historical source anchors describe current intent.

### Remaining Scope

R15 stays open for full immutable resolver/policy/plugin context, complete rule
grammar/semantics, custom helper conformance, all-route safe diagnostics, persisted
flag compatibility and provider/package users. Definition capture requires host
coordination with catalog writers; unrelated metadata/mapping/filter elements,
watermark objects, provider handles and plugin/store instances are not frozen by
the new top-level configuration copy. Rule failures after prior acknowledgements
are Partial, not rollback. No native cancellation or atomic provider recovery.

Continue remaining B1 ownership/adapters before B2 staged promotion/cursor
agreement. Wider P1-P5 native/provider-run recovery, config/security, migration
execution admission, lifecycle/drain, performance and release gates remain. The
goal is unfinished; do not turn this verified slice into phase completion.

## Required Editor-Owned Default Resolution - 2026-10-03

Previous goal turn classification: progress. The planning refresh established a
2,895-execution source baseline and the next R15 action. This increment adds
required resolution under B1/P3-09/P5-05/07 without completing the five-phase goal.

### Implemented

- Required import defaults normalize once; colon expressions no longer become
  literal text through double normalization. Legacy DefaultsManager.Resolve also
  preserves prefixes and selects the supplied editor's resolver registry.
- Weak-keyed editor resolver ownership preserves custom registration across editor
  switches. Registry snapshots coordinate registration/selection; plugin callbacks
  run outside registry locks. Required resolution bypasses metadata-only value
  caches, reads actual row property context and denies null/non-finite values,
  exceptions, invalid normalization/envelopes and reported error/warning fallbacks.
- Scoped required diagnostics suppress raw BaseDefaultValueResolver rule/value/
  exception logs and propagate nested failure. Date/formula/expression error paths
  report formerly silent fallbacks; valid arithmetic/dates/literals remain covered.
  Cancellation is checked around synchronous resolution and between defaults.
- Safe typed row failure retains earlier acknowledgements, stops whole-sync retry
  and preserves successful dates/cursors. Existing required catalog admission is
  retained. DEFAULTS-ADMISSION.md records legacy behavior and remaining limits;
  source/harness/direct/nested import/sync skills are synchronized.

### Verification

Initial focused 15 regressions failed before implementation (exit 1), including
false-success default outcomes and lost expression semantics. Cache regression
setup was corrected to seed the actual resolver manager before that authoritative
red run: `$env:TEMP/BeepDM-required-resolver-red.log`.
Later valid dot-style fixture quotes separate numeric arguments rather than using
an ambiguous decimal segment; this is not claimed as a parser defect/fix.
An initial full matrix exposed one existing timeout fixture returning Ok after
SpinUntil-based waiting. BusyWait now uses Stopwatch duration, matching the
production timeout basis; no production threshold policy was loosened.

Final focused net9 run: 36 passed (34 new resolver cases + two existing threshold
cases), zero failures/skips; `$env:TEMP/BeepDM-required-resolver-qualified.log`.
Final source-built Windows SDK 10.0.401 solution run: **2,997 passed**, nine
successful runs, no failures/skips. Forms 221, Setup 232, Studio 66; Migration 160
and Reliability **666 each** net8/net9/net10. Command:
`dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'`.
Log: `$env:TEMP/BeepDM-required-resolver-final.log`. Generated tracked Setup
bin/obj outputs are restored; pre-existing source/docs changes remain intact.

### Remaining Scope

R15 is not closed: repeated config/catalog capture, closed literal types, complete
immutable resolver/policy context, both-direction catalog admission before writes,
complete grammar/built-in/custom semantics, all-route diagnostics, persisted flag
compatibility and provider/package users remain. Legacy profile/config/accessor
state and result caches remain best-effort; registered callbacks are trusted code.
Required bounds do not establish a sandbox, interruptible providers or row rollback.
Next finish these B1 gates, then B2 promotion/cursor agreement. All wider P1-P5
provider/native recovery, config/security, migration ownership, lifecycle, performance
and reproducible release gates remain as planned. No broad checkbox is completed.

## Planning-Only Source Review And Matrix Refresh - 2026-10-03

See REVIEW-REFRESH-2026-10-03.md for prioritized findings, change boundaries,
owners, dependencies and acceptance tests under existing P1-P5 IDs. No runtime,
test or skill implementation was changed in this pass. Existing dirty work was
preserved. Required defaults-catalog admission already present in the worktree
now has local all-TFM verification; resolver/default ownership and broader gates
are not inferred complete from these nine cases per TFM.

Source-built Windows SDK 10.0.401 verification, both exit 0:
- net9 FrameworkReliabilityTests: 632 passed, no failures/skips;
  `$env:TEMP/BeepDM-enhancement-review-net9.log`.
- full solution: 2,895 passed across nine project/target runs, no failures/skips;
  `$env:TEMP/BeepDM-enhancement-review-full.log`. Forms 221, Setup 232, Studio 66,
  Migration 160 and Reliability 632 each net8/net9/net10.

Both commands disabled GeneratePackageOnBuild and GenerateDocumentationFile.
Generated tracked Setup bin/obj changes were restored because those paths were
clean before this pass. Pre-existing source/docs edits remain. No clean pack,
Unix/provider tests, new concurrency reproductions or phase checkbox changes.
Next: B1 resolver/defaults/adapters, B2 promotion/cursor agreement; C/D remaining
configuration/security and migration ownership, provider/release inventory in
parallel. Complete five-phase implementation remains unfinished.

## Durable File Reject Claims And Actual Replay - 2026-10-03

Previous goal turn classification: progress. The planning-only review reproduced
two unsafe replay-success failures and a corruption exception-type mismatch,
and changed the next action. This increment delivers R14 file recovery under
P1-10/12/P2-05/06/P3-09; the complete five-phase goal remains active.

### Implemented

- Models adds durable run/reject identity, closed recovery states, optional
  IImportRejectRecoveryStore/IImportReplayDataSource and typed replay counts.
  Quality admission captures destination snapshots and shared sync run identity.
- File reject storage validates managed identity/version/state/payload, rejects
  duplicate IDs and coordinates operator preparation, CAS claims, acknowledgement,
  reconciliation and dismissal. Managed records omit arbitrary RawRecord JSON.
  Unresolved/corrupt evidence cannot be cleared; legacy coordinate marking cannot
  select managed or ambiguous cross-run records. Payloads are closed typed values
  with depth/node/field/aggregate limits and no CLR activation. Public malformed
  JSON retains JsonException with a safe message instead of exposing raw bytes.
- DataImportManager performs one actual write after persisted claim, current
  quality and actual generated destination rehydration. No transform/default
  reapplication, automatic retries, missing-store success or legacy-stage guessing.
  Optional provider replay-key capability receives durable RejectId. Bulk recovery
  only admits prepared rows and cannot hide blocked claims behind an empty list.
- Provider acknowledgement is separate from completion persistence. Cancellation
  after Ok preserves acknowledged counts; uncertain writes or failed save responses
  remain reconciliation-required, never blindly replayed. Cleanup uses a separate
  cooperative five-second token; invalid claim snapshots are not released.
- BeepSync's direction-aware row replay captures current record policy/engine and
  requires Bidirectional for reverse. It does not complete failed runs, clear
  checkpoints, reevaluate attempt thresholds or advance cursors. Source/harness/
  direct/nested Codex guidance and current trackers are synchronized.

### Verification

Windows, SDK 10.0.401. New inventory: **51 ImportRejectRecoveryTests plus four
SyncRejectRecovery cases per TFM**. Coverage includes concrete transformation and
ClassCreator/Roslyn rows, exact current target identity/metadata, correction/current
quality, provider replay keys, repeated coordinates across runs, concurrent
managers and child-process claims/reload, supported typed values, uncertainty,
cancellation, explicit failures, pre/post-commit adapter failures, corruption,
dismissal and advisory observer isolation. Existing corruption preservation now
passes its original exact exception/byte assertions.

Focused source-built net9 run: **56 passed**, zero failures/skips, exit 0,
including the existing corruption regression. Log:
`$env:TEMP/BeepDM-recovery-focused.log`.

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Final source-built matrix: **2,868 passed**, zero failures/skips, exit 0.
Nine successful runs: Forms 221/net8, Setup 232/net9, Studio 66/net9,
Migration 160 each net8/net9/net10 and Reliability 623 each net8/net9/net10.
Log: `$env:TEMP/BeepDM-recovery-final.log`. Documentation generation is disabled
to preserve existing dirty docs.xml. This run's tracked Setup bin/obj output
changes are restored; prior dirty source and untracked workers are preserved.

### Remaining Scope

Read Engine Editor/Importing/REJECT-RECOVERY.md for host-owned authorization,
provider-state evidence/fencing, payload privacy/keys and conservative claims.
Custom capabilities remain trusted; file mutation leases are not provider-run
leases or atomic target/reject commits. Native-channel triage, live-provider
commit/transaction recovery, complete intent, cursor/Completed/promotion agreement,
scalable retention, Unix and package consumers remain unqualified. P1-10/11/12,
P2-05/06 and P3-09 stay open. Next B1 default/resolver qualification, then B2
promotion/cursor agreement; other configuration/migration/lifecycle/conformance
and all P4/P5 gates remain. No broad completion checkbox is changed.

## Required Attempt Thresholds And Failed-Run Evidence - 2026-10-03

Previous goal turn classification: progress (R14 actual record admission and its
contracts were verified). This increment implements attempt thresholds and typed
failed checkpoint evidence under existing R14/P1-10/12/P2-06/P3-09 scope, not the
complete five-phase goal. Durable reject/operator recovery and wider gates remain.

### Implemented

- Models adds explicit BatchThresholdEnabled (default true), independent Required/
  Advisory ThresholdFailureMode and closed immutable SyncBatchThresholdResult.
  Engine captures rule/engine/key/limits/numeric policy before target mutation.
  Missing required dependencies and invalid configuration admit no import.
- Thresholds execute once after admitted imports per attempt, including partial
  failures. Actual attempted rows across admitted directions form the denominator;
  rejected rows count, warnings/evaluation failures are distinct, empty rate is zero
  and empty attempts still evaluate. Exact ContinueRun/AbortRun strings and finite
  limits replace silent skips/coercion. ContinueRun cannot override numeric excess
  or an existing import failure. Required errors stop completion/retry; Advisory
  warnings retain explicit evidence. Rule policies are fresh per solve.
- Captured retry/checkpoint identity is used for Running/Failed/Completed, rather
  than recomputing a rebound caller policy hash after writes. Failed/cancelled
  checkpointed runs after acknowledged startup persist versioned typed actual
  counts, uncertainty, quality/store outcomes and threshold evidence. Cleanup uses
  an independent five-second cooperative token; no forced interruption is claimed.
- Failed-save status/null/throws retain original counts via CheckpointStage=Failure
  and ImportResult. Completion-save failure does not try rewriting possibly committed
  Completed state. Storage validates explicit evidence/counts, preserves corruption,
  makes Failed terminal artifacts immutable and blocks restart until reconciliation.
  Reconciliation now uses measured import row/write/quarantine totals; broader
  default/conflict/SLO metrics remain unqualified.
- Record-only regression fixtures explicitly opt out of thresholds. Example06 now
  requires a host-registered engine, uses explicit quality/quarantine/checkpoint
  policy and documents strict attempt-end semantics. Legacy method signatures remain.
  Source/direct/nested sync/import/mapping skills and harness guidance are refreshed.

### Verification

- Initial targeted run reproduced **three red defects**: missing required engine
  permitted a write, a thrown threshold published Success and AbortRun hid the
  acknowledged import result. The core corrected run passed all three.
- New inventory per TFM: **45 SyncThresholdTests and 13 failure-evidence persistence
  cases**. Coverage includes actual generated mapped rows/both directions, real/empty
  denominators, strict actions, missing/disappearing/throwing rules, required/advisory
  decisions, caller rebind, fresh captured limits, cancellation, every save status,
  null/throwing adapters, restart, corruption preservation, terminal immutability,
  separate-process typed reload and actual Windows replacement denial.
- Final focused net9.0 run: **58 passed**, exit 0. Logs under `$env:TEMP`:
  `BeepDM-threshold-red.log` and `BeepDM-threshold-focused-final.log`.
  Earlier combined sync/outcome/storage run passed 144 cases before expansion.
  A fixture initially hit legacy Fatal classification rather than exercising retry;
  explicit transient triage now proves two independently owned threshold policies.
- Full source-built Windows matrix, SDK **10.0.401**: **2,703 executions**, nine
  successful project/TFM runs, zero failures/skips. Forms 221/net8, Setup 232/net9,
  Studio 66/net9, Migration 160 each/net8/9/10, FrameworkReliability **568**
  each/net8/9/10. Log: `$env:TEMP/BeepDM-threshold-final.log`. Command:

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

### Remaining Scope And Compatibility

All **11** affected skill folders pass quick_validate.py; source/direct/nested
import/sync copies and the shared owned-metadata mapping sections agree. All nine
framework documents resolve **104 local links and 42 line anchors**. Scoped tracked
whitespace checks and **26** relevant new/plan-file checks pass. This turn's generated
tracked Setup bin/obj outputs were restored; pre-existing dirty Engine/Models
docs.xml and unrelated work remain. These checks are not provider/release evidence.

Read [threshold/failure contract](../../DataManagementEngineStandard/Editor/BeepSync/THRESHOLDS-AND-FAILURE-EVIDENCE.md).
Enabled DQ policies that relied on missing-threshold skip must register the required
rule, explicitly disable BatchThresholdEnabled or choose Advisory. New policy fields
can change context fingerprints; preserve/migrate prior evidence, never overwrite
to force replay. Checkpoint-disabled runs have no equivalent durable guarantee.
Captured descriptors/identity do not freeze all schema/filter/provider intent or
trusted plugins. Durable reject IDs/triage/replay, default/catalog/resolver safety,
live-provider recovery, promotion/cursor agreement, provider-run admission, bounded
streaming, lifecycle, reproducible packages/CI/API checks and Unix remain open.
No broad phase checkbox is completed and the full active goal remains required.

## Actual Record Quality Admission - 2026-10-02

Previous goal turn classification: progress (R16 existing-target mapped admission
was verified). This increment implements R14's actual record-admission core, not
the complete five-phase goal. Required thresholds and durable reject/failure
recovery remain open; no broad completion-checkbox changes.

### Implemented

- Models adds optional IImportRecordAdmission, closed typed outcomes and explicit
  required/advisory record policy without changing existing public signatures.
  Runtime admission instances are excluded from configuration serialization.
- Ordinary QualityRules and the optional gate execute after actual transformations
  and configured defaults, before provider retries. Captured field/action/policy
  descriptors persist across batches; missing fields, invalid configuration and
  required evaluation failures never imply pass. Opaque custom batch helpers cannot
  bypass configured admission. Rule FailureMessage/raw exceptions are not published.
- Sync captures rule keys, engine/context, actual directional entity, limits and
  record policy before imports. Both mapped directions use the actual transformed
  payload, and engine outputs must be Boolean. This policy is explicitly record-only;
  it does not repair the legacy batch-threshold stage.
- Block/Quarantine/Warn, evaluation failures and reject-store failures have distinct
  counts. Quarantine requires acknowledged persistence; built-in datasource stores
  check InsertEntity acknowledgements. Named sync channels require an already-open,
  existing target and are qualified write-only sinks, not operator replay stores.
- Bidirectional outcomes retain forward acknowledgements after reverse failure or
  cancellation. Denied admission is not an uncertain provider write, and required
  rejection stops successful cursor advancement and blind whole-run retry.

### Verification

- Three initial ordinary-import regressions reproduced bypassed Block, required
  evaluation failure and absent quality calls on actual transformed records.
- New coverage: **33 ImportQualityAdmissionTests and 18 SyncRecordQualityTests per
  TFM**. Dictionaries, Expando, DataRow and POCOs; missing/ambiguous fields; actual
  defaults/transforms; policy/context mutation; strict Boolean outputs; required/
  advisory failures; provider retries; reject-store acknowledgements/failures;
  forward/reverse failure and cancellation; serialization compatibility.
- Final focused net9.0 run: **51 passed**, exit 0. Logs retained under `$env:TEMP`:
  `BeepDM-quality-red.log` and `BeepDM-record-quality-focused-final.log`.
- Full source-built Windows matrix, SDK **10.0.401**: **2,529 executions**, nine
  successful project/TFM runs, zero failures/skips. Forms 221/net8, Setup 232/net9,
  Studio 66/net9, Migration 160 each/net8/9/10, FrameworkReliability **510**
  each/net8/9/10. Log: `$env:TEMP/BeepDM-record-quality-final.log`. Command:

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Source/direct/nested import and sync skill copies agree; mapping's shared owned-
metadata section agrees while its existing surrounding installed guidance is
preserved. All **11** affected skill folders pass quick_validate.py. All nine
framework planning documents resolve **102 local links and 43 line anchors**;
scoped tracked-file whitespace checks and 18 new/plan-file checks pass. This turn's
generated tracked Setup bin/obj changes were restored; pre-existing dirty
Engine/Models docs.xml and unrelated work remain intact. Metadata/link checks do
not establish runtime or provider conformance.

### Remaining Scope

See [quality contract](../../DataManagementEngineStandard/Editor/Importing/QUALITY-ADMISSION.md)
and [roadmap B1](ENHANCEMENT-ROADMAP.md). Required batch-threshold intent, denominator,
empty-run behavior and safe failure publication still need implementation. Returned
counts are not durable failed-checkpoint evidence. Reject indices are run-local,
not stable replay identities; no atomic target/reject transaction is claimed.
Synchronous callbacks are observed at boundaries, not forcibly interrupted or
sandboxed. Custom store/rule implementations, implicit default catalogs, resolver
diagnostics, live providers, mapped creation, package consumers and Unix remain
outside this slice. The full five-phase plan remains required.

## Existing-Target Mapped Sync Admission - 2026-10-02

Previous goal turn classification: progress (R17 core source-sensitive generation
was verified). This turn implements R16's existing-target metadata/shape binding,
not the complete five-phase goal. No broad completion-checkbox changes.

### Implemented

- Sync preflight captures actual provider structures and returns caller-owned
  metadata with resolved handles. Translator BindEntityMetadata captures each
  direction separately, populates both destination EntityFields/SelectedDestFields
  and normalizes mapped names/types to actual provider authority. Empty declaration
  strings cannot stand in for missing metadata.
- Models adds opt-in RequireBoundMappingMetadata. The import helper validates
  actual pairs, unique target assignments, entity/datasource identity, required
  non-auto-increment coverage and bound shape before execution. Legacy unbound
  validation/creation remains unchanged.
- Mapping-aware preflight accepts supported explicit renames instead of requiring
  same-name overlap. Exact key pairs are included once; half matches do not suppress
  the real pair, reverse does not unconditionally duplicate it and malformed pairs
  are not silently dropped.
- Both bidirectional configs are bound and validated with actual generated target
  shapes before either writes. Actual generation failures/unsupported properties
  cannot defer reverse admission until after forward writes. Provider existence
  disagreement rejects before sync writes, and import rechecks destination presence.
- Bound mode requires an existing mapped target, even if creation is enabled; it
  never creates from source structure or declared mapping types. Preflight null/
  non-Ok/exception results no longer fall through to import, and caller cancellation
  returns Cancelled without admitting writes.

### Verification

- Initial targeted net9.0 run reproduced three defects: explicit rename veto,
  duplicate reverse key pair and half-key suppression. The first corrected run
  passed all three. A fourth targeted regression then reproduced actual reverse
  generation failure after one acknowledged forward insert; generation/shape
  admission now rejects before that write. No mocked transform or type-cache seed
  substitutes for the actual ClassCreator/Roslyn route.
- SyncMappingAdmissionTests adds **27 cases per TFM**. Coverage includes actual
  forward/reverse payloads and actual type authority, provider casing, captured
  metadata/pair mutation, invalid reverse required/generation coverage, missing/
  ambiguous/duplicate/foreign/null metadata, incomplete pairs, required fields,
  cancellation, safe preflight provider/observer exceptions and no-DDL missing
  target behavior. Direct bound import tests verify no uncertain write on denial.
- Final net9.0 focused sync/import run: **86 passed**, exit 0. Retained logs:
  `$env:TEMP/BeepDM-mapped-red.log`, `BeepDM-mapped-shape-red.log` and
  `BeepDM-mapped-focused-final.log`.
- Full source-built Windows matrix, SDK **10.0.401**: **2,376 executions**, nine
  successful project/TFM runs, zero failures/skips. Forms 221/net8, Setup 232/net9,
  Studio 66/net9, Migration 160 each/net8/9/10, FrameworkReliability **459**
  each/net8/9/10. Command:

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Retained final log: `$env:TEMP/BeepDM-mapped-final.log`, exit 0. Compiler/analyzer
warnings and deliberate failure-fixture diagnostics remain. Documentation generation
was disabled to preserve pre-existing dirty Engine/Models docs.xml. This turn's
tracked Setup bin/obj outputs are restored after verification; unrelated changes
and the old untracked persistence-worker tree remain intact.

Eleven source/harness/direct-installed/older nested skill folders pass metadata
validation. Beepsync/importing entrypoints and sync references match their installed
copies byte-for-byte; the mapping owned-metadata section matches while preserving
older surrounding installed guidance. Nine framework documents have 101 valid
local links and 47 in-range source line anchors. Scoped Git whitespace and direct
checks on new code/tests/framework docs pass. Setup tracked build artifacts are clean.

### Contract And Remaining Work

Contracts, current roadmap/review and source/direct-installed/older nested skills
are refreshed for mapped admission. Read Engine Editor/BeepSync/STORAGE-AND-OUTCOMES.md
for exact compatibility/creation/ownership limits. Metadata captures are mutable
and require coordination during capture. The preflight now rejects unsupported
graphs instead of retaining live provider-owned metadata. Custom generators are
trusted host code and may run again; precompilation is not a frozen generator or
proof of future row conversions. No distributed provider lease or live schema-drift
refresh, native import upsert, mapped DDL/reload, immutable policy/context intent,
clean-pack/package-consumer or Unix qualification is claimed.

R14 actual post-transform/pre-write quality admission and required threshold
semantics remain the next B1 increment, with lower-level R15 default/resolver
qualification. Do not replace row enforcement with a threshold catch-only patch.
R13 promotion/remaining R12 durable cursor agreement follow. Remaining configuration
facades, migration admission, provider inventory, lifecycle, performance and release
gates across all five phases remain in scope. The goal stays active.

## Source-Sensitive Generated Types And Cache Ownership - 2026-10-02

Previous goal turn classification: progress (R18 cloning/capture prerequisite was
implemented and verified). This turn implements R17's core source/type identity,
not the complete five-phase objective. No broad completion-checkbox change.

### Implemented

- DMTypeBuilder captures destination fields before code generation and requires
  the exact full requested type. Source is generated on each lookup so schema and
  current custom-generator output cannot be bypassed by a bare-name hit. Empty/
  missing fields fail explicitly. Both object creation overloads return their
  local instance instead of rereading a mutable static result.
- Roslyn compiled identity is requested name plus SHA-256 of exact source, with
  single-flight admission. Full names select exactly; simple names must be unique,
  never substring matches. Legacy assembly-only requests retain null Item1 when
  absent, while entity generation requires an actual Type. Emit diagnostics expose
  IDs/positions without raw source/message values or Console.Error echo.
- EntityTypeFactory captures full supported metadata/table annotations and uses
  source identity too, preserving Entity inheritance. Fixed diagnostic text does
  not echo caller metadata. Its legacy failure/null contract remains.
- Shared bounded cache coordinates admission without holding locks during factories.
  Failures evict only their own attempt; invalidation/clear during work cannot
  republish old entries. Compiler/factory each retain at most 256 completed entries,
  excluding in-flight work. Public compiler count includes in-flight entries.
- Public typeCache and signatures remain, but engine entries are an advisory
  source-v2 mirror (256 engine-owned entries). Legacy bare-name/manual entries
  cannot override metadata and unrelated caller entries are not trimmed. This is
  a documented behavior change, not proven package compatibility. Public static
  MyType/MyObject remain last-result conveniences, not runtime authority.
- Existing strict import mapping fixtures now use actual ClassCreator/Roslyn
  targets instead of pre-seeded recording types, inspecting actual provider payloads.
  Added ConfigUtil/GENERATED-TYPES.md and updated source/direct/nested mapping skills,
  framework plans, metadata/import contracts and canonical agent evidence.

### Verification

Four initial actual-compilation regressions reproduced stale property definitions,
cross-namespace inner-cache reuse, changed-source reuse and substring type selection.
Initial fixed focus passed 31; expanded focus passed 53 (18 generated-type cases,
eight actual Engine cache-helper cases and 27 existing import transformation cases).
Controlled gates cover active invalidation/clear, eviction, single-flight, retry,
factory lock boundaries and recursive same-key failure; no timing sleeps used.

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Exit 0; `$env:TEMP/BeepDM-type-identity-final.log`: **2,295 executions**, nine
successful Windows project/TFM runs, zero failures/skips. SDK 10.0.401. Forms 221/
net8, Setup 232/net9, Studio 66/net9, Migration 160 each net8/9/10, Reliability 432
each net8/9/10. Adds 26 cases per TFM (78 executions) to the prior 2,217 matrix.
Warnings/fixture diagnostics remain. Setup tracked bin/obj were clean at entry;
only this run's generated changes were restored. Existing dirty docs.xml preserved.
Seven affected skill folders pass UTF-8 quick_validate. Importing direct/nested
copies match source; mapping metadata sections match while their existing bodies
remain preserved. Nine framework plans have 103 valid local links and 50 valid
line anchors. Scoped git and direct checks of untracked source/test/docs/plans pass
whitespace validation; tracked Setup generated paths are clean after restoration.

### Remaining Gates

Assembly.Load remains noncollectible: cache-reference bounds do not prove loaded
assembly memory/lifecycle limits, source-byte/CPU/concurrency budgets or P4-07
completion. Generation on every lookup still needs performance measurements.
Direct public-cache consumers, custom generated state, other code/helper/rule paths,
supported Unix/live providers and package-only compatibility remain unqualified.
R16 must now bind and validate owned forward/reverse mapping metadata before either
direction writes. R14 record/threshold admission and remaining R15 defaults/context
qualification follow, then promotion/cursor agreement and the rest of the full plan.

## Metadata Cloning And Owned Snapshot Prerequisite - 2026-10-02

Previous goal turn classification: progress (source-derived R17/R18 changed the
next implementation action). This turn implements the R18 prerequisite, not the
full framework objective. No broad phase checkbox changes; goal remains active.

### Implemented

- EntityField.Clone terminates through a detached shallow value copy, retaining
  IDs/properties without copying PropertyChanged subscribers. Legacy public
  EntityStructure.Clone remains shallow; no implicit deep-copy behavior change.
- Additive Models EntityMetadataSnapshot.Capture owns the supported metadata
  graph, including fields/keys/parameters/relations/indexes/filters, nested options
  and aliases. It preserves key identity, order, nulls and built-in comparer
  semantics. It does not merge distinct key descriptors by name or regenerate IDs.
- Unsupported/derived objects, custom comparers, cycles and depth/size excess fail
  with fixed safe errors. Future mutable Entity model members require explicit
  copy paths. Supported collections are bounded before allocation; no arbitrary
  custom activation/serialization fallback. The result is mutable and capture
  needs caller synchronization; this is not automatic immutable run admission.
- Added metadata contract and source/harness/direct/nested importing/mapping skill
  guidance. Existing installed mapping content is preserved; only the metadata
  section is added. Governed migration's separate snapshot/format is unchanged.

### Verification

Initial field-clone regression failed on the direct recursive IL call before
invocation, avoiding a stack-overflow test-host crash. After correction, focused
net9.0 passed 26 cases; a scalar-property preservation case brought the final
inventory to 27 per TFM. Two expanded-test compile attempts exposed a missing
nested FieldMergeStrategy alias in the new fixture; corrected before final tests.

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Exit 0; retained `$env:TEMP/BeepDM-metadata-final.log`: nine successful Windows
project/TFM runs, **2,217 executions**, zero failures/skips. Forms 221/net8,
Setup 232/net9, Studio 66/net9, Migration 160 each net8/9/10, Reliability 406 each
net8/9/10. This adds 81 executions to the prior 2,136 matrix. Warnings and fixture
diagnostics remain; no Unix/live-provider/clean-pack/package-consumer claim.
Tracked Setup bin/obj were clean at entry; only this run's generated changes were
restored. Pre-existing dirty docs.xml and unrelated user edits are preserved.
SDK rechecked: 10.0.401. Seven affected skill folders pass quick_validate in UTF-8
mode; importing direct/nested files match source, and added mapping sections match
without overwriting their pre-existing different bodies. Nine framework plans have
102 valid local links and 52 valid line anchors. Scoped git and direct untracked
whitespace checks pass. Setup generated paths are clean after restoration.

### Next Required Work

Implement R17 schema-sensitive type generation using actual compiler/payload
regressions, then R16 mapping-aware preflight/owned forward-reverse binding before
either direction writes. Do not use the new copy API as proof that admission is
wired. R14 quality gates, R15 catalog/resolver qualification, promotion/cursor
agreement and all remaining five-phase requirements stay open. Legacy custom
metadata needs an explicit supported adapter/policy, not silent fallback.

## Framework Review And Plan Refresh - 2026-10-02

Planning-only response to the framework review request. No Engine/Models source,
test, project, installed-skill or completion-checkbox changes in this refresh.
The full implementation objective remains active; this is not its completion.

Re-inspected sync translation/preflight, import validation/quality admission,
dynamic type generation, metadata cloning, promotion and package project files.
Expanded R16 with same-name validation, either-side forward key detection and
unconditional reverse key insertion. Added source-derived R17 (schema-insensitive
static type cache) and R18 (recursive EntityField.Clone, reached by MergeFields
and CloneStructureOnly). Shallow EntityStructure.Clone is documented separately
from the already captured governed migration snapshots; do not conflate them.

Updated CURRENT-REVIEW, ENHANCEMENT-ROADMAP, Phase 3 and master entry point. B1 now
orders safe metadata cloning/capture, schema-sensitive actual type generation,
mapped forward/reverse admission, real record DQ/thresholds and defaults/adapters.
Each step retains existing phase IDs and explicit acceptance/compatibility gates.
Required rejection/cursor semantics, quarantine acknowledgement, cache ownership
and copied mutable members require decisions before their implementation.
Corrected a stale Phase 3 phrase implying the built-in custom callback still falls
back; strict transformations are already implemented, quality admission is not.

Verification baseline inspected, not rerun: retained
`$env:TEMP/BeepDM-transform-final.log` has nine successful test runs, totaling
2,136 executions (221, 66, 160, 160, 160, 232, 379, 379, 379). Prior source-built
Windows SDK/TFM evidence remains the latest; no new clone/cache regressions, live
providers, Unix or package consumers were executed. No crash-inducing recursive
clone reproduction was attempted. Framework-plan link/line-anchor and whitespace
checks are the validation for this planning refresh, not runtime qualification.
Validation passed: nine planning documents, 101 local link targets and 53 line
anchors, with zero missing/out-of-range targets or whitespace issues. Scoped
git diff --check also passed; direct checks include the currently untracked plans.

## Strict Import Transformation Admission - 2026-10-02

Previous goal turn classification: progress. The planning pass expanded R14 and
identified R15 from concrete source paths. This turn implements the strict
transformation core; the full five-phase objective remains active and no broad
completion checkbox changes.

### Implemented

- Models-owned optional IDataImportTransformationOutcome, immutable stage/result
  evidence and safe ImportTransformationException preserve legacy signatures.
  Built-in configured stage failures no longer return the input. Null custom or
  typed outcomes deny writes; record payloads/raw exceptions do not enter returned
  transformation diagnostics. Existing legacy custom helpers execute once but can
  still conceal their own failure; their contract requires honest outcomes.
- Batch/manager RecordsTransformationFailed is included in RecordsFailed, not
  write attempts or uncertain provider work. Earlier acknowledgements survive a
  later transformation failure. Cancellation is checked across stage/helper
  boundaries, never represented as a provider call that did not happen.
- Strict field mapping avoids swallowed compiled-step errors, rejects unknown
  transforms and defaults conversion to Reject. Deliberately registered conversion
  warning/fallback and mapping skip/null policies remain opt-ins. Strict mapping
  does not apply hidden defaults; the configured ApplyDefaults stage owns them.
- Configured static defaults use their actual definition, with strict dictionary,
  dynamic, DataRow and public POCO field assignment rather than legacy Util setter
  failure masking. Implicit catalog lookup uses an editor-owned helper instead of
  rebinding static DefaultsManager. Catalog failure semantics and rule resolution
  ownership/diagnostics still need a dedicated qualification increment.
- Typed transformation failure stops whole-sync retry even with zero acknowledged
  writes and preserves success dates/cursors. Two sync tests prove zero-write and
  earlier-acknowledgement behavior with actual concrete default conversion failure.
- Added TRANSFORMATION-OUTCOMES.md, interface XML, source/harness/import/mapping/sync
  skill guidance and installed direct/nested Codex updates. Importing skill/reference
  copies match the canonical source; removed its accidental outer Markdown fence.

### Verification

Initial six regression tests reproduced input writes/false completion before the
fix (after correcting test compilation). Expanded focused net9.0 run passed 29
cases: 27 concrete/import/adapter cases plus two sync outcome cases.

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Exit 0; `$env:TEMP/BeepDM-transform-final.log`: **2,136 executions**, zero failures
or skips across nine Windows project/TFM runs with SDK 10.0.401. Forms 221/net8,
Setup 232/net9, Studio 66/net9, Migration 160 each net8/9/10, Reliability 379 each
net8/9/10. This adds 29 cases per TFM (87 executions) to the prior 2,049 matrix.
Mapping fixtures use uniquely keyed precompiled recording targets; they do not
prove the independent Roslyn compiler contract. No live-provider/Unix/pack consumer
verification is claimed. Compiler/analyzer warnings remain.

Tracked Setup bin/obj were clean at entry; only this run's generated changes were
restored. Pre-existing dirty Engine/Models docs.xml and unrelated changes remain.
Eleven affected source/harness/direct/nested skill metadata validations passed in
UTF-8 mode (the validator's Windows default encoding could not read existing
Unicode harness text). Import/sync installed skill/reference copies match source.
Ninety-two framework-plan local link targets exist. Scoped git whitespace checks
and direct checks for 14 untracked plan/source/doc/test files passed.

### Next Required Work

B1 is not complete: connect normal import QualityRules and sync record DQ, define
required/advisory outcomes and real reject counts, and close threshold fail-open
behavior (R14). Qualify implicit default-catalog reads, resolvers and custom adapters
(remaining R15). New R16 is source-backed: forward/reverse translator mappings omit
EntityFields/SelectedDestFields before validation/generation. Bind actual target
metadata and add payload-level mapped-sync regressions; do not weaken validation.

Then deliver B2 recoverable promotion/cursor agreement, remaining config/secret
routes, migration token/target admission and recovery, lifecycle/provider/grammar
conformance, bounded execution and all reproducible release gates. No narrowing
of the plan, universal rollback, row undo, exactly-once or all-route redaction claim.

## Framework Review And Plan Refresh - 2026-10-02

Planning-only response to the framework review/enhancement-plan request. No runtime,
test, public API, storage format or installed skill changes in this pass. Existing
dirty worktree changes are preserved; the full implementation goal is not complete.

Re-inspected normal import/sync write paths, concrete transformation helpers,
schema promotion/fingerprinting, migration admission, workflow/query persistence,
timer lifecycle, NFEL and packaging. Expanded R14: configured record-quality rules
are disconnected from normal writes and threshold reject counts stay zero. Added
R15: concrete transformation errors return the input, which can then be inserted.
The existing import accounting fixture mocks transformation as identity. These are
source-backed findings, not new fault-injection reproductions.

Updated CURRENT-REVIEW, ENHANCEMENT-ROADMAP, framework/root trackers and affected
Phase 1/3 verification criteria. Existing work IDs and completed slices remain;
no completion checkbox changed. Split next delivery into B1 record-quality/strict
transformation admission and B2 recoverable promotion/cursor agreement. Configuration
facades, migration admission/recovery, lifecycle/conformance, bounded execution and
all release gates remain in scope. Provider inventory and build/CI proceed in parallel.

Validation: inspected retained `$env:TEMP/BeepDM-sync-terminal-final.log`; nine
successful runs total 2,049 executions (221 Forms, 232 Setup, 66 Studio, 160
Migration per TFM, 350 Reliability per TFM). This confirms prior evidence only;
tests were not rerun. Framework-plan link validation checked 90 local targets with
no missing files. Scoped git whitespace validation passed; direct trailing-whitespace
and EOF checks also passed for all nine currently untracked framework documents.
Historical root-tracker missing links and unrelated source changes were not altered.

## Typed Sync Storage And Completion Boundaries - 2026-10-02

Previous goal turn classification: progress. The review refresh corrected stale
findings and selected mandatory terminal acknowledgement/outcome isolation as the
next slice. This turn implements that slice and qualifies the carried-forward sync
storage increment. The full five-phase goal remains active; no broad item is marked
complete from this local matrix.

### Implemented

- Built-in sync schema/version/checkpoint updates use AtomicFileStore, strict
  versioned envelopes and closed typed cursor values shared with import watermarks.
  Per-schema upsert/delete owns the whole update; whole SaveSchemas is replacement.
  Hashed exact identities, immutable versions, run/offset/reconciliation guards and
  validated run-owned clearing preserve foreign/corrupt/unresolved evidence.
- ConfigPath selects a host root; explicit roots/custom persistence remain available.
  Ambiguous shared old roots, untagged old cursors and raw-name artifacts require
  explicit migration rather than silent fresh state or guessed types. Valid metadata
  arrays can upgrade; schema backups validate snapshots and use unique atomic paths.
- Reproduced six missing/non-string/case-ambiguous schema identity defects: model
  constructors could invent IDs or Newtonsoft could coerce them. Read validates one
  actual string identity before deserialization. Three casing fixtures preserve
  exact identity values across eligible legacy upgrade.
- Startup and Completed require ISyncPersistenceAcknowledgement when enabled.
  Completed records retain actual acknowledged counts/context. Failed/null/throwing
  terminal saves return SyncCheckpointFailureResult with counts/status/run ID and
  reconciliation required; no Success/cursor publication or blind terminal retry.
  Cancellation after an acknowledged completion does not undo its stored outcome.
- SLO/alert/reconciliation/history/property notifications and audit-unsubscription
  failures are separate advisory diagnostics. DiagnosticFailed isolates each observer;
  LastRunDiagnosticFailures exposes a per-run copy of operation/exception type.
  Rule/observer/logger exception details are not logged. Later diagnostic stages
  proceed after earlier failures; success is not reclassified.
- Added STORAGE-AND-OUTCOMES.md, corrected persistence interface XML and sync model
  claims, refreshed .cursor sync instructions/reference, added the missing harness
  sync skill and updated both installed Codex beepsync copies. Four skill metadata
  validations pass; metadata checks are not runtime/behavior verification.

### Verification

Six initial terminal/diagnostic regressions failed before the fix. The later identity
run reproduced all six new corruption cases (nine existing cases passed). Focused
sync verification passed 83 cases before the last two outcome and nine identity
cases were added. An intermediate full matrix passed 2,022 executions.

Final source-built verification, Windows SDK 10.0.401:

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Exit 0: **2,049 executions**, zero failures/skips across nine project/TFM runs.
Forms 221/net8, Setup 232/net9, Studio 66/net9, Migration 160 each net8/9/10,
FrameworkReliability 350 each net8/9/10. Log:
`$env:TEMP/BeepDM-sync-terminal-final.log`.
There are 86 additional reliability cases per TFM since the 264-case connection
baseline: 64 sync storage cases and 22 additional sync outcome cases (six carried
forward plus sixteen in this slice). Fixtures include real process typed reload,
concurrent process updates, actual Windows replacement denial retaining Running
bytes/counts, restart replay rejection, immutable versions, cancellation and
throwing notification/diagnostic/subscription adapters. They do not prove Unix,
live provider recovery, clean checkout pack or package consumers.

Tracked Setup bin/obj changes generated by the runs are restored; pre-existing
dirty Engine/Models docs.xml are preserved. Previously blocked untracked
project-root persistence-worker cleanup is not retried or bypassed.

Scoped diff checks and 83 local documentation references pass. A global diff check
reports unrelated new EOF blank lines in the two project Claude.md hosting-rule
files; those independently changed instructions are read and preserved unchanged.

### Remaining Full Scope / Next Actions

P2-05/06 remain open for broader configuration callers, durable schema/cursor plus
checkpoint agreement, promotion/backup recovery, complete context/policy compatibility
and supported platform qualification. A file update lease is not provider execution
ownership or a multi-artifact transaction. No offset/key replay is implemented.
Running/uncertain evidence is conservatively preserved, not automatically repaired.

Next address R14 required DQ gate evaluation outcomes and R13 staged promotion/full
intent identity with interrupted-write recovery. Then remaining configuration facades
and migration token/target admission, owned progress and intermediate DDL recovery.
P1 manual/live-provider/per-record recovery, P2 lifecycle/secrets/timers, P3 provider/
helper/rule conformance and every Phase 4/5 performance/release gate remain required.
Legacy report/SLO count placeholders are not complete accounting; storage and
terminal failure counts are the current acknowledged evidence. Do not convert this
slice into a universal success, idempotency, transactional DDL or release claim.

## Connection Protection And Save-Observer Increment - 2026-10-02

Previous goal turn classification: progress. The planning-only refresh added the
roadmap and source-backed R11; this turn implements/qualifies the next connection
slice. The full five-phase goal remains active, with unchanged completion scope.

### Implemented And Qualified

- Qualified existing Models-owned optional IConnectionConfigurationPersistence
  and per-runtime IConnectionSecretProtector/context/cipher/key-provider contracts.
  ConfigEditor supports captured constructor injection; runtime options snapshot
  policy references instead of changing a process-global default.
- Built-in whole-container protection covers the explicit named/compound whitelist
  including ConnectionString/ParameterList, HTTP containers and authentication
  URLs. DPAPI CurrentUser is Windows-only; host-injected AES-256-GCM binds version,
  key ID and connection Guid purpose. Key storage/rotation/access remain host-owned.
  No failure falls back to plaintext; source credential containers are deep-cloned.
- Connection save preserves catalog Save(false) and preparation/storage failures;
  protected fallback updates validate existing state before replacement, and load
  changes memory only after complete successful validation. Legacy void saves now
  throw. Custom catalog acknowledgements and security policy remain adapter-owned.
- Reproduced four real-file old-key overwrite cases: sync/async whole save, remove
  and add could overwrite unreadable encrypted evidence. Both coordinated catalog
  mutation paths now validate every existing credential record before changes.
  Redacted export remains key-independent; encrypted export requires decrypt/reprotect.
- New built-in catalogs/exports use package/record version 2.0. Reads support legacy
  1.0 plaintext/named-field DPAPI upgrade. Unknown versions, duplicate properties,
  invalid record identity and 1.0 records carrying new opaque payload are rejected.
  Import rename decrypts and rebinds a new Guid before protection. Old unchecked
  readers/fallback-array downgrade are not safe compatibility guarantees.
- Reproduced observer escape on five repository operations. Captured subscribers
  now run individually outside scope locks; exceptions cannot change durable
  outcomes or skip later observers. Additive concrete NotificationFailed carries
  only operation/scope/exception type. Diagnostic subscribers and logger failures
  are isolated and messages never include raw observer details. Invalid scopes
  are rejected rather than mutating a dictionary concurrently.
- Added 102 cases per TFM: 56 credential, 20 save acknowledgement, 16 protected
  catalog and 10 observer cases. Includes actual filesystem AES child-process
  reload with externally supplied test key, key rotation/loss, tampering, Guid
  binding, legacy upgrade, malformed formats, cancellation, replacement denial,
  compound sentinel coverage, redaction without keys, facade/runtime isolation,
  reentrant/slow observers and saved bytes despite observer failure.
- Added Security/README.md, updated persistence and authoritative plan/agent docs,
  and enhanced .cursor/.harness plus directly installed root/nested configeditor
  skills. Four skill folders pass metadata validation (not behavioral proof).
- Worker-copy targets now skip outer multi-TFM builds; prevents copying test runtime
  output into the project root. Existing generated root binaries remain untracked:
  tool policy blocked recursive removal, so nothing was deleted or bypassed.

### Verification

Before-fix reproductions: five notification cases fail in
`$env:TEMP/BeepDM-notification-before.log`; four old-key evidence cases fail in
`$env:TEMP/BeepDM-catalog-key-before.log`. No failing tests were removed/suppressed.
Focused protection/notification run passed 61; the completed net9 reliability
run passed 264. Final source-built matrix:

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Exit 0 on Windows SDK 10.0.401. Forms 221/net8, Setup 232/net9, Studio 66/net9;
Migration 160 and Reliability 264 each on net8/net9/net10. Nine completed test
runs, **1,791 executions**, zero failures/skips. Log:
`$env:TEMP/BeepDM-connection-protection-final.log`.
This run's tracked Setup bin/obj and generated Models docs.xml were restored;
pre-existing dirty Engine docs.xml and unrelated changes remain preserved.

### Remaining Gates And Next Increment

P2-05/07/08 stay open: inventory other config/environment/serialization/export/
diagnostic routes, qualify custom adapters and real host key management, define
fallback mixed-reader migration, verify Unix/package consumers, backup/recovery
and remaining lifecycle ownership. This is not an exhaustive security audit.
Notifications are synchronous refresh signals and may arrive out of commit order;
slow observers still delay their caller. Public mutable lists/settings are not
universally thread-safe. Rotation needs retained old keys and host backup policy.

Next implement whole-update coordination and strict acknowledged reads/writes for
remaining configuration/BeepSync schema/version/checkpoint storage, then version
typed policy/cursor restart without falsely enabling unsupported execution modes.
Keep P3-06 concurrency/intermediate-DDL recovery, P1 manual/live recovery, timer/
plugin drain, provider/rule conformance, all Phase4 and release gates intact.
No phase or full goal is declared complete from this local matrix.

## Acknowledged Migration Storage Increment - 2026-10-02

Previous goal turn: progress. Current-source verification and source-backed R10
changed the next dependency to acknowledged, corruption-preserving persistence.
This increment implements that migration boundary. The full five-phase goal
remains active; broader store/security/recovery/provider/release gates are open.

### Implemented

- Models-owned IMigrationHistoryPersistence, PersistenceWriteResult/status and
  IJsonSnapshotCodec are additive; IConfigEditor/IJsonLoader signatures remain.
  ConfigEditor implements acknowledged history methods. Original void history
  save/append now propagate failure rather than logging it as apparent success.
- JsonLoader's stable snapshot codec ignores ambient JSON defaults and rejects
  cycles/additional content. Migration history coordinates complete updates,
  validates stored identity/type/version/records and preserves corrupt evidence.
  New version-one hashed paths avoid sanitized-name collisions. Valid legacy
  history is copied on first update, leaving original bytes intact. Mixed old/new
  writers and cross-OS/case-renamed legacy discovery are explicitly unsupported.
- Planning remains available offline and exposes PlanPersistenceStatus/error code.
  Preview tokens reserve intent in-process, not durable execution start. Governed
  execution requires explicit store acknowledgement; unsupported/null results fail
  closed before DDL. Saved start/Running markers gate provider attempts. Retry
  admission uses a sentinel because the shared pipeline ignores hook exceptions.
- Failed post-DDL saves stop subsequent work, retain independently counted
  acknowledgements, return failure/reconciliation requirements and do not mark a
  completed success. Primary provider failure survives secondary save failure.
  Restarted Running and corrupt/empty/wrong-token checkpoints block replay.
- Added 11 migration and 26 persistence/scheduler cases per TFM: real filesystem
  plan/checkpoint load and completed resume in a separate process, process append,
  corrupt/foreign/unsupported data, legacy preservation, serializer/replacement
  failures, cancellation and unsupported/custom acknowledgement adapters.
- A full-matrix run exposed a real mixed sync/async catalog lease timeout. Source
  showed sync-over-async pool dependence. Atomic sync methods and catalog sync
  facades now perform sync I/O; lease-held read/replace sections do not suspend.
  A separate-process test caps the pool at two workers and proves contended mixed
  catalog updates complete. Async lease waiting remains cancellable; filesystem
  critical sections are synchronous, not a fully async/interruptible disk API.
- Updated normative persistence/migration docs, 8 repository/directly installed
  root/nested migration/configuration skill entrypoints, and current plans/agent
  guidance. All 8 affected skill folders pass quick_validate; that checks metadata,
  not behavior. Skills describe actual APIs/limits, not pending guarantees.

### Verification

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Final source-built exit 0, Windows SDK 10.0.401: Forms 221, Setup 232, Studio 66,
Migration 160 per net8/net9/net10 (480), FrameworkReliability 162 per TFM (486).
Total **1,485 executions**, zero failures/skips. Log:
`$env:TEMP/BeepDM-history-storage-final.log`.
Intermediate failures included a cancellation-token/local-name collision, worker
IDataSource namespace, planning's formerly automatic mandatory checkpoint write,
one assertion expecting a provider method name instead of its actual error, and a
ConfigEditor fixture missing its logger. Corrected before final verification.
The mixed catalog timeout was investigated and addressed, not hidden by relaxing
the timeout, reducing concurrency or removing the test.

### Remaining Gates

P3-04 captured snapshot persistence and P3-05 identity are verified on the local
Windows matrix. P3-06 remains open for concurrent-token admission/owned progress,
provider-normalized intermediate state and real partial-DDL reconciliation.
P2-05 remains open for other configuration/environment/sync stores, backup/recovery
and cross-OS qualification; P2-06/07 for sync envelopes and credential policy.
Whole-history Save is deliberate replacement, not optimistic merging. Diagnostic
and imperative summary records remain best-effort. Saved acknowledges a completed
local filesystem operation, not power-loss durability or signed audit integrity.
P1 manual/live recovery, lifecycle/timers, provider/rule conformance, streaming,
retention/benchmarks, clean pack/API consumers and release gates remain required.
No live database, Unix, security audit or package-consumer gate was verified.
Prior worktree edits were preserved; only this run's generated tracked Setup
bin/obj changes were restored. Pre-existing Engine docs.xml remains untouched.

## Current-State Review Refresh - 2026-10-02

This pass reviews and plans only; it adds no runtime implementation or completed
work IDs. The existing enhancement objective and outstanding gates remain active.
Prior runtime/source/skill changes were preserved.

Reviewed migration history/writer/checkpoint boundaries, connection saving and
secret protection, BeepSync persistence, Forms timers, NFEL tokenization and
packaging. Added source-backed R10: swallowed migration history/checkpoint saves,
read failures becoming empty histories, uncoordinated appends and sanitized-name
collisions. These observations are not new fault-injection reproductions.

Updated CURRENT-REVIEW.md and the master/phase 2/3 plans. Acknowledged,
corruption-preserving persistence is now the next dependency for reliable
migration recovery. Defined before/after-DDL failure semantics, compatibility
boundaries and filesystem/process regression gates. Previously delivered
schema/hash/payload work is no longer presented as an unstarted slice. Skill
updates remain a delivery gate once the proposed contracts are implemented;
no skill now claims these proposed guarantees.

Verification: initial no-build/no-restore rerun passed 1,354 executions but used
an older Setup binary (212 cases), so it was not accepted as current-source
evidence. Rebuilt with:

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Exit 0, Windows SDK 10.0.401: Forms 221, Setup 232, Studio 66, Migration 149 per
net8/net9/net10 and FrameworkReliability 136 per net8/net9/net10. Total 1,374
executions, zero failures/skips. Logs: `$env:TEMP/BeepDM-framework-review-source.log`
and the superseded `$env:TEMP/BeepDM-framework-review-refresh.log`.
Only this run's tracked Setup bin/obj changes were restored; Engine docs.xml
was already dirty and remains untouched. No live-provider, Unix, clean-pack,
API-consumer, security-audit or performance gates were verified in this pass.

## 2026-10-02: Phase 1 Correctness Increment

Goal remains implementation of the entire five-phase plan. This increment does
not complete the plan or all Phase 1 acceptance gates.

### Implemented

- Fixed the setup full-install test fixture to resolve manifest/blob URLs against
  a real feed URL. The captured failure proved fixture drift, not a service bug.
- Added Models-owned ImportExecutionResult/ImportOutcome while preserving legacy
  IErrorsInfo return signatures. Only Completed maps to Ok; cancellation, failed,
  and partial imports do not. Counts distinguish records from write attempts.
- Counted acknowledged import writes only. Per-record retry does not replay
  successful rows; exceptions/null/ambiguous acknowledgements prevent blind retry.
  Removed manager-level whole-batch retry and redundant batch materialization.
- Added native IUpsertDataSource capability for the pipeline sink. Failed update
  no longer triggers insertion. Sink failures propagate as PipelineWriteException
  with explicit replay safety; cancellation and partial/unknown writes are not
  retried. Existing retry overloads remain, with additive token-aware overloads.
- Added opt-in IRDBSource sink transactions. Transactional counts publish after
  commit, and failed commit remains eligible for rollback cleanup. Default sink
  writes are explicitly nontransactional and may be partial.
- Deferred transactional UOW/OBL acceptance and AfterSave until database commit.
  Failed writes/commit preserve dirty state; confirmed rollback restores generated
  identities. Secondary rollback errors do not hide the original failure.
- Kept edits during commit pending as updates; retained accepted baselines for
  later edits and repeated reject operations. Notification errors after confirmed
  commit are warnings rather than failed database acknowledgements.
- Blocked sync success/cursor advancement on failed/cancelled imports, prohibited
  automatic whole-sync replay after acknowledged/uncertain writes (including
  reverse-import failures), and guaranteed rule-audit subscription cleanup.
- Rejected unsupported Sequence/CompositeKey CDC modes before target writes.
  Timestamp queries include an invariant-format upper window bound.
- Added Studio and FrameworkReliabilityTests to the solution. The new project
  targets net8.0/net9.0/net10.0. Replaced a racy Forms capture poll with task-based
  synchronization after a full run exposed the race.
- Updated repository importing/ETL/UOW skills and directly installed Codex
  importing/ETL/sync/OBL skills, including existing nested BeepDM copies.
- Made only .plans/framework Markdown trackable; other local plans remain ignored.

### Verified

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -v:q
```

| Project | Passing executions |
|---|---:|
| MigrationManagerTests (net9.0) | 103 |
| SetupWizardTests (net9.0) | 232 |
| FormsManager.Tests (net8.0) | 221 |
| StudioRepositoryTests (net9.0) | 66 |
| FrameworkReliabilityTests (40 cases per TFM, three TFMs) | 120 |
| Total | 742 |

Zero failures/skips in the final run. Existing compiler/analyzer warnings remain.
Regression providers are recording mocks, not live databases. This proves the
covered orchestration/tracking paths, not every external provider's guarantees.

### Required Next Work

- Finish P1-10/P1-12: define provider-backed manual resume/reconciliation.
  Automatic retries are bounded safely, but restarting a partially written import
  manually is not automatically idempotent. Persisted per-record diagnostics and
  error-store integration need stronger coverage.
- Add real-provider tests for transaction commit/rollback, generated identities,
  and ambiguous acknowledgements. Verify token support and platform behavior.
- Complete Phase 2: independent DI containers/scopes, datasource coordination,
  atomic persistence, typed durable cursors, and consistent secret protection.
- Complete Phase 3: provider conformance and canonical persisted migration plans.
  The insensitive create-column hash characterization is still unchanged.
- Complete Phase 4: end-to-end streaming/backpressure, benchmarks, telemetry,
  bounded retention. Source import still materializes the full dataset.
- Complete Phase 5: CI, clean-checkout package builds/consumers, API checks,
  samples, pinned dependencies, and removal of tracked build outputs. Test-project
  inclusion and affected skill updates are early progress, not phase completion.

## 2026-10-02: Current-State Review Refresh

- Reviewed the current dirty worktree without changing runtime implementation.
- Re-ran the solution command above on Windows with SDK 10.0.401: 742 passing
  executions, zero failures/skips. No additional integration guarantees inferred.
- Added CURRENT-REVIEW.md with current source anchors, prioritized delivery order,
  explicit decisions and phase acceptance evidence. Original findings remain
  historical; existing Phase 1 fixes are not reclassified as missing work.
- Separated completed automatic retry/counting work (P1-06) from pending manual
  reconciliation, live-provider proof and diagnostics (P1-10/P1-11/P1-12).
- Added name/GUID close outcome work (P2-09), credential fallback evidence and
  persistence implementation boundaries. Corrected the active skill-root plan.
- This review completes planning only, not the active implementation goal or
  remaining release gates. No new phase is marked complete.

## 2026-10-02: Phase 2 Persistence Increment

### Implemented

- Added AtomicFileStore: serialize before mutation, same-directory flushed
  temporary files, bounded replacement retries, cancellable 30-second per-file
  leases and full read/modify/write coordination across cooperating processes.
  Persistent sidecars are intentionally not deleted. Read fallback coordinates
  through the lease to avoid Windows replacement-time missing snapshots.
- JsonLoader now writes complete serialized snapshots and propagates failures;
  shared reads preserve missing-file defaults but reject corrupt files explicitly.
- Unified synchronous/asynchronous catalog paths, coordinating the entire target
  update across instances/processes. Corrupt/version/scope errors do not become
  empty catalogs. Fixed precedence so Project overrides User then Machine.
  Promotion/import are source snapshots plus a coordinated target update, not
  multi-file transactions. Disposal rejects subsequent public operations.
- Added coordinated import error/history JSONL snapshots with context validation,
  isolated-folder constructors and collision-resistant case-insensitive keys.
- Added version-1 watermark envelopes with closed scalar/composite/array types,
  invariant encoding, timestamp kind/offset preservation and context validation.
  In-memory cursors now honor cancellation and null values consistently.
- Legacy sanitized import files are explicitly rejected and left unchanged.
  No guessed cursor conversion or silent corrupt-store reset was introduced.
- Added a test-only child-process worker and 56 new persistence/catalog cases
  per TFM. Tests cover failure preservation, cancellation, readers/writers,
  process updates, killed lease owners, typed restart and rejected invalid data.
- Updated repository configuration/importing skills and installed configeditor/
  importing root and existing nested Codex copies. All six skills validate.

### Verified

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -v:q --logger 'console;verbosity=normal'
```

Windows, SDK 10.0.401: Migration 103, Setup 232, Forms 221, Studio 66,
FrameworkReliability 96 each on net8/net9/net10 (288). Total: **910 executions**,
zero failures/skips. Existing compiler/analyzer warnings remain. The focused
net9 persistence run also passed 56 cases. Full log:
`$env:TEMP/BeepDM-phase2-solution.log`.
The concurrent-reader snapshot regression then passed three additional isolated
net9 runs; the initial Windows race was fixed rather than relaxing the assertion.

### Remaining Gates And Limits

- Phase 2 remains in progress: independent DI/container lifetimes, datasource
  lifecycle/close outcomes and the complete secret-protection audit are not fixed.
- Higher-level config managers can still catch/log lower-level errors. Their
  acknowledged persistence contract and broader store coverage need follow-up.
- Typed import storage does not prove BeepSync schema-policy restart behavior
  or enable unsupported sequence/composite execution modes.
- Unix and remote filesystems were not tested. The lease is for cooperating
  local processes, not a distributed lock or a power-loss durability guarantee.
- Normal failures clean up temporary files; abrupt process death can leave an
  unselected temporary file. Automatic backups, migration tools, cross-OS legacy
  filename discovery and retention policies are not implemented.
- JSONL updates currently validate/rewrite complete files, so append cost grows
  with history. Streaming/retention work remains Phase 4, not a solved benchmark.
- Phases 1 recovery, 3 migration/provider contracts, 4 performance/observability
  and 5 release gates remain required. The full implementation goal stays active.

## 2026-10-02: Framework Review And Plan Refresh After Runtime Changes

Review/planning only; no runtime or test-source edits in this pass. Existing dirty
implementation changes were preserved. Refreshed CURRENT-REVIEW.md and reconciled
the master/phase plans rather than creating a competing roadmap.

### Verification

```powershell
dotnet test tests/FrameworkReliabilityTests/FrameworkReliabilityTests.csproj -f net9.0 --no-restore -p:GeneratePackageOnBuild=false --filter FullyQualifiedName~RuntimeIsolationTests -v:q --logger 'console;verbosity=normal'
```

Windows, SDK 10.0.401: compilation failed with CS0246 at
RuntimeIsolationTests.cs lines 137 and 252. IAssemblyHandler is in
TheTechIdea.Beep.Tools; the test lacks that import. No tests executed.
Log: `$env:TEMP/BeepDM-review-runtime-tests.log`.
Full solution, other TFMs, live providers, Unix, benchmarks and packaging were
not re-verified. The earlier 910-execution result predates the runtime increment.

### Plan Changes

- Recognized AddBeepRuntime, per-runtime default seeding, explicit path failures
  and correct Closed results as existing implementation, not missing features.
- Prioritized current compilation, global datasource cache/removal ownership,
  migration schema hashes, save acknowledgement and secret-path gaps.
- Added P2-10 timer shutdown/overlap proof, P3-09 helper/rule conformance and
  P4-08 Forms/plugin lifecycle performance workloads. These are proposed gates,
  not completed implementation or reproduced production incidents.
- Kept the five-phase architecture, public compatibility boundaries, release
  evidence requirements and provider-specific limitations. No new completion
  checkbox or goal completion was recorded.

## 2026-10-02: Phase 2 Runtime And Datasource Ownership Increment

### Implemented

- Fixed RuntimeIsolationTests' missing Tools namespace and verified the deferred
  AddBeepRuntime registration increment across all configured reliability TFMs.
  Provider-owned Singleton/Scoped/Transient graphs and per-runtime defaults no
  longer capture one process-wide live runtime. Existing component overrides are
  preserved, but do not replace internals of the factory-created graph.
- Added EditorDataSourceRegistry: editor-keyed state, single-flight name/GUID
  creation, owned compatibility-list publication, generation invalidation and
  isolated cleanup. Normal helper/editor creation no longer registers globally.
- Unified sync/async/local creation, setting connection/driver/metadata and the
  configured GUID before publication. Configuration failures release unpublished
  instances; recursive construction fails rather than deadlocks, while different
  dependencies can be resolved without holding a registry lock.
- Removed the stale last-source cache. Name/GUID removal detaches references,
  invalidates pending creation, closes/disposes the source and permits a later
  replacement if its connection remains configured. Cleanup failure still attempts
  disposal and returns false; it does not resurrect detached resources.
- Coordinated editor open/close with removal/disposal. Synchronous provider removal
  callbacks defer source release until the admitted operation returns. Direct
  helper disposal targets the exact owned generation, not a newer replacement.
- Made service configuration one-shot/coordinated and assembly completion
  retryable after a loading exception. Failed direct Configure releases its partial
  graph and becomes terminal. Runtime cleanup cancels/disposes its token source, avoids concrete
  editor graph double-disposal, and continues releasing independent resources
  after cleanup/logger failures. Editor shutdown closes its registry first.
- Added 40 runtime/lifecycle cases per TFM (22 runtime, 18 datasource), using
  recording mocks/in-memory fixtures and controlled construction/close gates.
- Added Services/RUNTIME-OWNERSHIP.md and repository beepdm-runtime guidance;
  updated installed root/nested registration/service/datasource skills and the
  main router. Marked historical supplemental references as historical. Nine
  affected skills pass quick_validate.py; its scope is metadata/scaffolding, not
  behavioral proof. Examples were checked against source namespaces/methods.

### Verified

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -v:q --logger 'console;verbosity=normal'
```

Windows, SDK 10.0.401: Forms 221, Migration 103, Setup 232, Studio 66,
FrameworkReliability 136 each on net8/net9/net10 (408). Total: **1,030 executions**,
zero failures/skips. Existing compiler/analyzer warnings remain. Log:
`$env:TEMP/BeepDM-phase2-runtime-solution.log`.
The focused net9 run passed 37 runtime/lifecycle cases before the final reentrant
callback and initialization regressions were added; the full run includes all 40
per TFM. The initial full run passed 1,024 executions; the final rerun above includes
the two additional initialization cases per TFM.
The 40-case focused net9 suite also passed three consecutive no-build repeats
before the final cleanup-getter assertions were strengthened. The final full
solution rerun above includes those stronger assertions on all three reliability
TFMs. Cleanup continues even when legacy source name metadata or custom editor
component getters are unavailable. Only this run's tracked Setup bin/obj changes
were restored afterward; pre-existing dirty source/documentation was preserved.

### Compatibility Limits And Remaining Gates

- P2-01/02/03/09 now have regression evidence and are checked. Phase 2 is not
  complete: P2-05/06/07 persistence acknowledgement, BeepSync policy durability and
  secret-path policy; P2-08 broader editor/plugin subscriptions/ownership; and
  P2-10 timer drain/overlap remain open.
- Legacy eager Build/AddBeepServices return a separate caller-owned runtime.
  This behavioral change requires host migration and package/sample verification.
  Transient aliases independently resolve runtimes; use IBeepService properties
  for one coherent transient graph. Same paths intentionally share persisted data.
- Explicit legacy static folder/file/ownerless cache APIs remain single-host
  compatibility paths, not normal runtime ownership. Public lists, ErrorObject,
  component setters and direct provider CRUD are not universally thread-safe.
- Non-cancellable constructors may finish after disposal and are released rather
  than published. This is not a general asynchronous-drain guarantee. A throwing
  constructor owns its partial resources. InitializationTimeout is not enforced.
- Live external providers, Unix, full security assessment, plugin unloading,
  benchmark budgets and package consumers were not verified. Phase 1 manual
  reconciliation and Phases 3-5 remain required. The full goal stays active.

## Current Framework Review And Plan Refresh - 2026-10-02

User request: review the framework and create an enhancement plan. No runtime
implementation or skill behavior was changed in this pass. Existing dirty
implementation changes and completion checkboxes were preserved.

### Reviewed And Planned

- Refreshed CURRENT-REVIEW.md to remove repaired runtime/test issues from the
  current findings, preserving historical evidence in this log and the master.
- Confirmed the schema-insensitive hash characterization and execution-time
  metadata resolution; specified P3-04/05/06 as the next vertical delivery slice.
- Inspected configuration Save(false) acknowledgement loss, raw fallback writes,
  secret-container/export omissions, direct sync/environment writes, timer
  generations/draining, whole-source buffering, NFEL token gaps/retention and
  machine-dependent packaging. These are source-backed findings, not newly
  reproduced provider/security/fault-injection tests.
- Reconciled existing phase work IDs and acceptance gates rather than creating
  competing plans. Added specific regression and compatibility requirements.
  Provider inventory and reproducible build/CI preparation start in parallel;
  release qualification remains an explicit final gate.

### Fresh Verification

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Exit 0 on Windows, SDK 10.0.401: Forms 221, Migration 103, Setup 232, Studio 66,
FrameworkReliability 136 each on net8/net9/net10. Total 1,030 executions with
zero failures/skips. Log: `$env:TEMP/BeepDM-framework-review-current.log`.
Documentation output was disabled to preserve pre-existing dirty docs.xml.
This run's tracked Setup bin/obj changes were restored; unrelated changes remain.

This does not verify clean-checkout packaging, standalone package consumers,
external live providers, Unix, security coverage or benchmark budgets. No open
phase work item was checked complete by this review, and no active implementation
goal was completed or paused on the basis of a planning request.

## Governed Migration Intent Increment - 2026-10-02

Previous goal turn classification: progress. The review refreshed authoritative
source findings and reran the baseline, selecting P3-04/05/06 as the next slice.
This increment implements that slice's capture/identity/payload contract; the
full enhancement goal remains active and its scope is unchanged.

### Implemented

- Added Models-owned MigrationEntitySnapshot with cloned desired/baseline schema
  and observed existence. Type/discovery and resolved/unresolved model plans
  capture it; relational operations carry the captured schema too.
- Added PlanHashVersion=2 canonical SHA-256 identity: field definitions, defaults,
  keys, relations/indexes/options, operation order, target fingerprint, retry/
  failure, governance and performance policy plus capability/readiness flags.
  Generated metadata GUIDs, cosmetic wording and lifecycle state are excluded.
  Explicit snapshot serializers ignore host-wide JsonConvert.DefaultSettings.
- Preview/execution operate on schema copies. Apply/approval/preflight reject
  changed identity or target. Approval options require ApprovedPlanHash; supplied
  policy/environment changes require an explicit concrete plan revision and
  re-approval. Existing IMigrationManager signatures are unchanged.
- Plan records add PlanArtifactJson; concrete LoadMigrationPlan validates it.
  Checkpoints capture a non-recursive ApprovedPlan; resume/rollback consume that
  captured intent instead of reconstructing it from step labels/reflection.
  Hash/token mismatch no longer overwrites the original checkpoint/plan.
  Legacy summary-only/old-hash intent is rejected even on completed resume.
- Restored skipped UpToDate dependencies as satisfied. Added a mixed-result
  regression and corrected continue-on-failure so failed steps never produce
  Success=true or an IsCompleted checkpoint; acknowledged successes remain counted.
- Added 46 migration-intent cases and expanded the whole migration suite to
  net8/net9/net10. Restart cases reset static stores and load recording history;
  payload assertions inspect actual schema/column/index options, not only names. A controlled progress callback also attempts to mutate a later
  checkpoint target; provider routing remains frozen from approved intent. Ambient host JSON defaults
  cannot rename/omit canonical fields or change the snapshot round-trip.
- Added PLAN-INTENT.md, revised migration README/example and source-cache comments,
  updated .harness/.cursor and directly installed root/nested Codex migration skills,
  and reconciled agent guidance/current plans. Four skill folders validate;
  metadata validation is not behavioral proof.

### Verification

```powershell
dotnet test BeepDM.sln -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
```

Final run exit 0 on Windows SDK 10.0.401: Forms 221, Setup 232, Studio 66,
Migration 149 each on net8/net9/net10 (447), FrameworkReliability 136 each (408).
Total **1,374 executions**, zero failures/skips. Log:
`$env:TEMP/BeepDM-migration-intent-final.log`.
Intermediate runs exposed snapshot collection append-on-deserialization (fixed
with ObjectCreationHandling.Replace), skipped dependency failure, a remaining
rollback caller of the old reconstruction helper (changed to captured intent),
and a fixture that did not fail CreateEntityAs as intended (corrected before the
final mixed-result run). No failures were suppressed or tests removed.

### Remaining Gates

- P3-05 is verified and checked. P3-04/06 remain open for acknowledged/fault-injected
  durable storage, concurrency/progress admission, provider-normalized baseline
  checks and exact expected intermediate states after partial DDL. Conservative
  baseline checks can reject own partially applied work; reconciliation is needed.
- Record/config persistence can still log rather than acknowledge save failure.
  Recording history reload is not child-process filesystem durability or a real
  database recovery proof. Provider/version conformance and P1 live recovery remain.
- Revision/load are concrete additive APIs; validate host wrappers and package
  consumers before release. Studio callers supplying different apply-time policy
  now fail closed rather than silently modifying reviewed intent.
- Hashes are identity, not authorization/signatures; mutable checkpoint progress
  still needs ownership/admission/audit treatment. Credential protection, broader
  lifecycle, streaming/performance and clean-package gates remain unfinished.
- No live external provider, Unix, security audit, benchmark or standalone package
  consumer was verified. Source/documentation changes existing before this turn
  are preserved; only this turn's tracked generated Setup bin/obj were restored.
  The objective is not complete; continue with remaining phase work.

# Forms Reliability Implementation Log

## 2026-10-03: Commit Ownership Increment

Full objective: implement the [Forms reliability/enhancement plan](RELIABILITY-AND-ENHANCEMENT-PLAN.md)
for FormsManager and platform-neutral UI contracts. This is progress toward that
objective, not a closeout of Stage A or the full plan.

### Authoritative Changes

- Optional Models contracts: `IEnlistedUnitofWork`, `IUnitofWorkCommitStage`,
  `IFormsCommitOutcomes`, `ICoordinatedBlockSave` and `ISafeWriteRetry`.
- Engine `UnitofWork<T>` prepares writes without nested begin/commit or early
  tracking acceptance; its wrapper forwards the optional capability. Active or
  unresolved commits reject overlapping commit/rollback and datasource/collection
  replacement. Receipt result data is a defensive copy; resolution is fail-fast
  and does not hold a monitor while invoking observers.
- Forms captures form/block/UoW/provider identities, groups providers by reference,
  uses shared Forms datasource admission, verifies targets again before commit,
  cleans up unsuccessful begin/prepare/commit exits, and reports per-block/provider
  outcomes. Unknown acknowledgement/resource outcomes block automatic Forms replay.
- Confirmed rollback restores staged generated keys without discarding pending
  edits. Confirmed commit accepts only the saved snapshot; later edits remain dirty.
  Observer failures cannot change confirmed write success into a write failure.
- Dirty saves account for missing/failed/unattempted targets. Retry configuration
  is honored only with an explicit no-effects guarantee; text-based retries are
  removed. Legacy UoWs report independent commits, not invented multi-block ACID.
- Forms.Tests now targets net8.0/net9.0/net10.0 and pins its previously resolved test
  packages. Microsoft.Data.Sqlite 9.0.6 is a test-only real provider dependency.
  SQLite tests delegate through an IDataSource test adapter, not the external Beep
  SQLite plugin, and verify file-backed rows from a second connection.

### Verified Results

Latest source-built Forms matrix: **239 passed per TFM**, zero failures/skips;
**717 executions** across net8.0/net9.0/net10.0. This is 18 new cases per TFM over
the preceding 221-test Forms baseline. Related existing `UnitOfWorkTransactionTests`:
**12 passed per TFM**, zero failures/skips; **36 executions**.

```powershell
dotnet test DataManagementEngineStandard/Editor/Forms.Tests/FormsManager.Tests.csproj -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q
dotnet test tests/FrameworkReliabilityTests/FrameworkReliabilityTests.csproj --filter 'FullyQualifiedName~UnitOfWorkTransactionTests' -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q
```

Logs: `%TEMP%/BeepDM-forms-ownership-matrix-4.log` and
`%TEMP%/BeepDM-forms-uow-regression-matrix.log`. Both commands exited 0.
Whitespace checks passed. New documentation links were checked separately.
This increment did not run the full solution test suite or certify UI adapters,
other provider plugins, distributed atomicity, Unix behavior or package consumers.
Existing compiler/analyzer warnings remain; no warning-free claim is made.

### Initial Qualification Failures Preserved

- `BeepDM-forms-ownership-tests.log`: test compilation exposed that generic UoW
  is not the non-generic Forms contract; fixtures corrected to use its wrapper,
  and required helper constructor arguments were supplied.
- `BeepDM-forms-ownership-tests-2.log`: 229 passed/2 failed. Null commit fixture
  had incorrectly substituted success; fixed to exercise actual null handling.
  Historical timeout-text retry expectation replaced with conservative no-replay
  behavior and a separate explicit-safe-retry qualification case.
- `BeepDM-forms-ownership-tests-3.log`: 233 passed on net8.0.
- `BeepDM-forms-ownership-tests-4.log`: test compilation caught nonexistent system
  variable getters; assertions corrected to verify the existing setter contract.
- `BeepDM-forms-ownership-matrix-1.log`: SQLite fixture compilation needed the
  namespace containing `DataBlockMode`; corrected without changing runtime logic.
- `BeepDM-forms-ownership-matrix-2.log`: 237 passed per TFM.
- `BeepDM-forms-ownership-matrix-3.log`: 239 passed per TFM; the fourth matrix also
  qualifies defensive receipt results and nonblocking completion admission.

### Remaining Work

Stage A is still open for transitive metadata/configuration snapshots, rollback of
propagated foreign keys, per-action audit/notification/lock cleanup, cross-form
commit graph cases and provider-owned transaction handles for external/raw
concurrency. Current IDataSource triple cannot prove ownership of an externally
opened transaction. See [commit contract limitations](COMMIT-OWNERSHIP.md).

Stage B was not implemented by the commit increment; see the subsequent query
increment below. C lifetime/registration/
timers, D scheduling/validation, E host conformance and F provider paging remain
open. G has only begun: test targeting/pinning and this contract documentation;
full regression/release evidence, warnings and Forms skill closeout remain.

Pre-existing migration/framework changes were preserved. No commit, PR or goal
completion was made for this increment.

## 2026-10-03: Managed Query Policy Increment

### Authoritative Changes

- Basic/enhanced queries (including already-Query-mode calls), immediate/deferred
  detail reads, count and source aggregate share one authorization/default/row
  policy boundary. Policy filters are not duplicated by basic delegation.
- Managed predicates use a closed, bounded AND compiler, declared field/type
  projection and invariant finite conversion. Unsupported grammar, missing
  parameters and arbitrary object values fail before row/scalar execution.
- Context/block policy containers and public query snapshots own their parameter
  dictionaries. Revision, target identities, defaults and field schema are
  rechecked after callbacks/awaits. Changed/cancelled counts do not return stale
  success. Failed detail reads no longer recurse into stale child masters.
- Optional parameterized scalar execution preserves existing IDataSource members.
  Legacy count builds quoted identifiers and dialect-specific safe literals,
  rejects invalid/fractional/overflow results and never fetches block records.
  Source aggregates accept only a closed declared-field grammar, not raw SQL.
- Shared AppFilter builders support NOT LIKE and quoted collections, preserving
  embedded commas/quotes and empty strings. Real SQLite executes both count modes
  and read/count/aggregate agreement against tenant-separated actual rows.

Compatibility and remaining security boundaries: [QUERY-POLICY](QUERY-POLICY.md).
This is not a full Stage B gate closeout or external provider qualification.

### Verified Forms Results

Latest source-built Forms matrix: **305 passed per TFM**, zero failures/skips;
**915 executions** across net8.0/net9.0/net10.0. This adds 66 cases per TFM to
the preceding 239-case commit matrix. Log:
`%TEMP%/BeepDM-forms-query-policy-matrix-3.log`, terminal exit 0.

The first full FrameworkReliabilityTests matrix passed **1,393 per TFM**, zero
failures/skips (4,179 executions), at
`%TEMP%/BeepDM-forms-query-framework-matrix.log`. The final source-built rerun
after aggregate/snapshot refinements also passed **1,393 per TFM**, zero
failures/skips, at `%TEMP%/BeepDM-forms-query-framework-matrix-2.log`, terminal
exit 0. Combined final qualification: **5,094 executions**, no failures/skips.
Existing compiler/analyzer warnings remain; no whole-solution, external plugin,
desktop adapter, Unix or package-consumer qualification is inferred.

```powershell
dotnet test DataManagementEngineStandard/Editor/Forms.Tests/FormsManager.Tests.csproj -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
dotnet test tests/FrameworkReliabilityTests/FrameworkReliabilityTests.csproj -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=minimal'
```

### Initial Qualification Evidence

- `BeepDM-forms-query-policy-build.log`: five compiler errors in initial new code
  (missing Linq import, incorrect security logging method, unreachable char arm);
  corrected before tests. Models compilation succeeded at that point.
- `BeepDM-forms-query-policy-baseline.log`: 237 passed/2 failed. The count SQL
  fixtures still expected interpolated quoted integers; corrected to quoted
  identifiers with typed numeric literals, without weakening filtering.
- `BeepDM-forms-query-policy-1.log`: new fixture compilation needed the Errors
  namespace import; corrected.
- `BeepDM-forms-query-policy-2.log`: 295 net8.0 passes.
- `BeepDM-forms-query-policy-matrix.log`: 295 passes per TFM.
- `BeepDM-forms-query-policy-matrix-2.log`: 305 passes per TFM, including aggregate
  grammar, owned public snapshots and denied immediate/deferred detail routes.
- Third Forms matrix qualifies the owned field-name/type projection used for
  compilation and aggregate validation; it is not a full metadata graph snapshot.

### Open Gates

Stage A remains open as above. B remains open for explicit auxiliary read/cache
policy targets and staged publication: legacy Get can publish Units before a
post-await rejection. C/D lifecycle, operation ordering and dirty-detail handling,
E host conformance, F paging/typed scalar results and G full skill/release
closeout remain. Raw data access and external plugins/adapters are not certified.

Documentation links checked: 35, zero missing targets. Whole-worktree whitespace
check reports pre-existing trailing blank lines in Engine/Models Claude.md;
unrelated files were preserved. Focused tracked-file diff checks passed; nine new
code/contract/query documentation files have no trailing whitespace. Tracked
SetupWizard bin/obj outputs remain unchanged. All invoked test sessions terminated.
No commit, PR or goal completion was made for this increment.

## 2026-10-03: Initial Lifetime And Timer Increment

### Authoritative Changes

- Dispose closes callback admission before clearing lookup and attempts each
  cleanup independently. Unregister and Dispose detach from captured UoWs, even
  after mutable block-source replacement. CleanupFailures retains bounded
  resource-specific diagnostics. Injected timer/performance helpers and UoWs
  remain borrowed; only default owned timer/performance instances are disposed.
- DisposeAsync waits for synchronous cleanup and manager-owned callbacks.
  WaitForPendingCallbacksAsync supports cancelling the wait, not cancelling
  underlying work. Self-drain is rejected instead of awaiting the owning callback.
  Item/LOV continuations recheck registration identity before publishing late errors.
- Default EventManager makes stored delegates inactive and attempts all recorded
  removals even when an accessor throws. Optional handlers now have recorded
  teardown actions. Physical removal failure remains observable, not certified
  detachment. Full partial-subscribe rollback is still open.
- Block names and direct-handler lookup use OrdinalIgnoreCase. Registry lookup
  is authoritative over borrowed/stale cache entries. Metadata/configuration
  preparation failure preserves a prior registration. This is not transactional
  rollback for every subsequent seeding/subscription/publication failure.
- Default FormMessageBus provides optional per-handler ownership leases. Forms
  clears only its own subscriptions, including same-form-name managers. A legacy
  bus receives an inert wrapper after close; physical wrapper removal is not
  available through its legacy contract.
- TimerManager keeps its parameterless constructor and accepts TimeProvider.
  Timers are prepared disabled and activated outside its ownership monitor.
  Entry identity blocks stale queued callbacks from firing/removing a replacement;
  snapshots cannot mutate scheduling. Serial activation failure preserves the
  prior timer. Overlapping failed activation never resurrects a retired entry,
  but does not guarantee preservation of the original. One-shot retirement,
  synchronous tick coalescing and bounded observer/scheduler diagnostics are covered.

Compatibility, ownership and remaining gates: [LIFETIME-CONTRACTS](LIFETIME-CONTRACTS.md).
No full Stage C gate is closed by this increment.

### Verified Results

Latest source-built Forms matrix: **332 passed per TFM**, zero failures/skips;
**996 executions** across net8.0/net9.0/net10.0. This adds 27 cases per TFM
to the preceding 305-case query matrix. Log:
`%TEMP%/BeepDM-forms-lifetime-matrix-2.log`, terminal exit 0.

FrameworkReliabilityTests: **1,393 passed per TFM**, zero failures/skips;
**4,179 executions**. Log: `%TEMP%/BeepDM-forms-lifetime-framework-matrix.log`,
terminal exit 0. Combined final qualification: **5,175 executions**, no
failures/skips. Existing compiler/analyzer warnings remain. No whole-solution,
external provider/plugin, desktop adapter, Unix or package-consumer qualification
is inferred.

```powershell
dotnet test DataManagementEngineStandard/Editor/Forms.Tests/FormsManager.Tests.csproj -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=normal'
dotnet test tests/FrameworkReliabilityTests/FrameworkReliabilityTests.csproj -p:GeneratePackageOnBuild=false -p:GenerateDocumentationFile=false -v:q --logger 'console;verbosity=minimal'
```

New lifetime fixtures use a manual scheduler and explicit barriers, not sleeps.
They exercise queued obsolete/deleted/disposed timers, activation failures and
inline ticks, snapshot ownership, observer/cleanup failures, reentrant replacement,
captured UoW teardown, stale callbacks, borrowed ownership, concurrent cleanup/drain,
late LOV results, callback self-close and same-form message subscription ownership.
These default-helper fixtures do not qualify arbitrary injected helpers.

### Initial Qualification Evidence

- `BeepDM-forms-lifetime-baseline.log`: 305 net8.0 passes before new fixtures.
- `BeepDM-forms-lifetime-1.log`: fixture compile errors for the LOV result
  namespace and optional CancellationToken in a Moq expression; corrected.
- `BeepDM-forms-lifetime-2.log`: 330 net8.0 passes.
- `BeepDM-forms-lifetime-matrix-1.log`: added fixture referenced a nonexistent
  Events property; corrected to subscribe the actual injected EventManager.
- Final Forms matrix above: 332 passes per TFM.

### Skill And Reference Qualification

Updated `forms`, `forms-helper-managers` and `forms-enhanced-data-operations`
entrypoints/references in repository `.cursor/` and direct installed folders in
`C:/Users/f_ald/.codex/skills/`. Corrected Models paths, obsolete APIs, generic
UoW registration examples, unsafe query-before-commit flow and lifetime claims.
Automatic skill selection remains enabled; no invocation policy was changed.

All six skill folders pass quick_validate.py with Python UTF-8 mode. Default
Windows decoding initially failed on existing Unicode in an entrypoint; the
validator itself was not modified. Reference fragments with explicit host inputs
were compiled in `%TEMP%/BeepDM-forms-skill-examples/Examples.csproj`, referencing
the actual Engine project, across net8/net9/net10. The initial build exposed a
missing ConfigUtil import for Errors; corrected in both copies. Final log:
`%TEMP%/BeepDM-forms-skill-examples-build-2.log`, terminal exit 0, zero warnings/errors.
This is API compilation, not execution of a real UI session.

### Open Gates

A/B remain open as above. C still requires atomic registration leases and
rollback, concurrent/reentrant registration, shared helper ownership and lifetime
admission/draining for public operations. D still requires generation-aware
scheduling, staged publication, dirty-detail preservation and validation identity.
E executable host/dispatcher conformance, F typed scalar/provider paging and G
full routing/release qualification remain open. Independent `.agents` and older
nested skill copies were not rewritten; duplicate discovery/routing is not closed.
Documentation links checked: 40, zero missing targets. Focused tracked-file diff
checks passed; five new lifetime/test/contract files have no trailing whitespace.
The three repository/installed reference pairs have identical content;
repository-only integration notes in the forms entrypoint were preserved.
Tracked SetupWizard bin/obj outputs remain unchanged. All invoked test/build
sessions terminated.
No commit, PR or goal completion was made for this increment.

## 2026-10-03: Event Subscription Ownership And Failure Unwind

### Authoritative Changes

- Added optional IOwnedUnitOfWorkEventSubscriptions without changing the existing
  IEventManager member set. Default EventManager returns independent leases for
  captured-source subscriptions; Forms captures/disposes its lease instead of
  using shared block-name teardown. Same-name managers no longer overwrite the
  default helper's teardown ownership.
- Every attempted add has removal recorded before the accessor runs. Delegates
  stay inactive during preparation. Failure attempts every recorded removal,
  leaves retained delegates inert and propagates the original exception, or
  aggregates attachment and cleanup failures. Missing public optional events are
  skipped; present accessor failures propagate instead of being silently ignored.
- Legacy name-keyed subscriptions use captured-source cleanup. Failed preparation
  preserves the previous subscription. If retiring the old subscription fails
  after publishing the new one, the new subscription remains live and the cleanup
  exception is reported; this is not a universal no-effects replacement contract.
- Failed fresh Forms registration removes its published lookup, unwinds recorded
  helper/direct subscriptions and attempts item/cache cleanup. Failing cache
  publication is invalidated even before lookup publication. Diagnostic observer
  failure cannot replace the original registration exception. A lease returned
  after retirement is disposed, without a fallback name-wide unsubscribe that
  could remove an unrelated legacy subscription.

See [current lifetime/compatibility contracts](LIFETIME-CONTRACTS.md).
The shared helper's event stream is still shared; leases isolate physical
subscription ownership, not per-manager notification routing.

### Verified Results

Latest source-built Forms matrix: **352 passed per TFM**, zero failures/skips;
**1,056 executions** across net8.0/net9.0/net10.0. Log:
`%TEMP%/BeepDM-forms-subscription-matrix-2.log`, terminal exit 0.
New SubscriptionOwnershipTests contributes **20 cases per TFM** over the
preceding 332-case matrix: base/optional failed attachments, exact cleanup,
combined failure evidence, inactive preparation, same-source/same-name leases,
captured legacy cleanup, same-name Forms owners, direct/helper/item/cache setup
failure, observer isolation and retirement during an owned factory call.

FrameworkReliabilityTests: **1,393 passed per TFM**, zero failures/skips;
**4,179 executions**. Log:
`%TEMP%/BeepDM-forms-subscription-framework-matrix.log`, terminal exit 0.
Combined qualification: **5,235 executions**, no failures/skips. Existing
compiler/analyzer warnings remain. No whole-solution, external provider/plugin,
desktop adapter, Unix or package-consumer qualification is inferred.

Initial evidence was green: subscription-baseline.log had 332 net8.0 passes;
subscription-1.log had 349 net8.0 passes; subscription-matrix.log had 352 passes
per TFM. All names have the `%TEMP%/BeepDM-forms-` prefix. The final Forms rerun
above includes the shortened source comment and final source line anchors.

### Skills And Open Gates

Updated forms/forms-helper-managers entrypoints and the helper reference in both
repository and direct installed Codex folders. The existing reference examples
were rebuilt against the updated Engine/Models on net8/net9/net10; this is
compilation, not UI session conformance. Validation/result evidence follows below.

Stage C remains in progress. Full prepare/publish/rollback of an entire block
registration, restoring a replaced prior block and shared item/form/system-variable
state, concurrent/reentrant registration/disposal, and all-public-operation
admission/draining are not closed. The reentrant factory fixture is one covered
case, not a proof of the general registration race gate. Legacy injected helpers
can still hide failures or use shared name-keyed ownership. A/B and D-G remain
open as recorded in the current plan; no stage/goal completion is claimed here.

Final checks: all six direct/repository skill folders pass quick_validate.py in
UTF-8 mode. Three reference pairs have identical content. The example rebuild
log is `%TEMP%/BeepDM-forms-subscription-examples-build.log`, terminal exit 0,
zero warnings/errors. Documentation links checked: 42, zero missing targets.
Focused tracked-file diff checks and new interface/test whitespace checks passed.
Tracked SetupWizard bin/obj outputs remain unchanged. All invoked test/build
sessions terminated. No commit, PR or goal completion was made for this increment.

## 2026-10-03: Prepared Registration Publication And Lifetime

### Authoritative Changes

- Added RegistrationLifetime with per-name reservations and captured-source leases.
  Prior lookup remains live during candidate preparation; same-name overlapping/
  reentrant changes reject without waiting. Close/unregister revoke pending setup,
  which cannot publish after retirement. Cleanup waits for external setup
  acknowledgement; registration preparation participates in async callback drain.
- Added optional IPreparedBlockItems/IBlockItemsRegistration and
  IGatedUnitOfWorkEventSubscriptions without changing existing helper member sets.
  Default items stage a fresh store/tab order, publish with the manager's pure
  owned-memory root action and restore the exact prior store on failure. Old lease
  retirement cannot remove replacement items. Shared item helpers reject cross-form
  same-name bindings instead of aliasing their stores.
- Default event delegates consult captured registration admission outside the
  helper monitor. Candidate/retired subscriptions cannot emit new helper events.
  Replacement requires dispatch gating as well as prepared items; owned-only or
  legacy helpers reject replacement before metadata writes/attachments. Legacy
  fresh registration retains best-effort cleanup, not universal hidden-side-effect
  rollback. Already-admitted observers may finish after retirement.
- Schema/source resolution occurs before block entity-type inference. Unpublished
  assignments are conditionally restored only if still reference-identical;
  successful assignments remain with borrowed UoWs/providers. Registration leases
  retain stable names/sources even if public block objects are mutated.
- Registry lookup/count/snapshot reads use the ownership gate. Blocks now snapshots
  the dictionary, not mutable block values. Default owned item helpers are disposed;
  injected helpers/UoWs/providers remain borrowed. Default current-block variables
  clear when registration teardown leaves no current block.
- Observer/old-teardown failures after complete publication remain in the cleanup
  ledger without undoing the new registration. Relationships/history persist on
  replacement, but schema/role/record-identity revalidation is not qualified here.
  No database durability, complete system-variable rollback, graph immutability,
  all-public-operation cancellation or UI dispatcher conformance is claimed.

See [current lifetime and compatibility contracts](LIFETIME-CONTRACTS.md).

### Verified Results

Latest source-built Forms matrix: **380 passed per TFM**, zero failures/skips;
**1,140 executions** across net8.0/net9.0/net10.0. Log:
`%TEMP%/BeepDM-forms-registration-matrix-3.log`, terminal exit 0.
AtomicRegistrationTests adds **28 cases per TFM** over the prior 352-case matrix:
seven failure boundaries retain the prior root/item state; exact replacement
retirement; blocked close/unregister acknowledgement; same-name overlap/reentrancy;
candidate event gating; observer/removal failures; shared item alias rejection;
legacy/owned-only helper rejection; reentrant enter/unregister; source/schema
resolution and rollback; mutable-name cleanup; invalid fields and closed stores.

FrameworkReliabilityTests: **1,393 passed per TFM**, zero failures/skips;
**4,179 executions**. Final log:
`%TEMP%/BeepDM-forms-registration-framework-matrix-2.log`, terminal exit 0.
Combined final qualification: **5,319 executions**, zero failures/skips. Existing
compiler/analyzer warnings remain; these results do not qualify the whole solution,
external provider plugins, desktop adapters, Unix or published package consumers.

The initial registration-baseline.log compiled, with 349 passes/3 failures on net8:
fixtures still expected the old close exception, early cache side effects and
replacement with an unsupported mock item helper. Updated those expectations to
ObjectDisposedException, no cache access before successful item preparation and
an explicit prepared-item mock respectively. Baseline-2 then passed all 352.
Registration-1 passed 373; matrix and matrix-2 passed 379 per TFM. The last capability
review tightened replacement from owned-only to gated events and added its explicit
rejection case, producing the final 380-case matrix above. Intermediate Framework
matrix also passed 1,393 per TFM. All logs use the `%TEMP%/BeepDM-forms-` prefix.

### Skills And Open Gates

Updated forms/forms-helper-managers entrypoints/references and corrected stale
RelationshipManager helper paths in all three Forms skill entrypoints, in repository
`.cursor/` and direct installed Codex folders. Preserved repository-only integration notes
and automatic skill invocation. The three Forms skill reference code fragments
are unchanged; their actual Engine/Models projection rebuilt across net8/net9/net10:
`%TEMP%/BeepDM-forms-registration-examples-build.log`, exit 0, zero warnings/errors.
This is API compilation, not execution of a UI session.

Stage C remains in progress: public query/commit/navigation and pre-RegisterBlock
source-factory admission/cancellation/draining, wider graph/system-state semantics
and custom helper qualification remain open. D requires request/record generations,
staged read publication, dirty-detail preservation and validation identity. A/B
and E/F/G remain open under the authoritative plan. Independent `.agents` and older
nested skill copies were not rewritten; duplicate routing is still unqualified.
No stage/goal completion or commit/PR is claimed by this increment.

Final checks: all six direct/repository skill folders pass quick_validate.py in
UTF-8 mode; three reference pairs have identical content. Current contract/plan/log
links checked: 50, zero missing targets. Focused tracked-file diff checks and seven
new/contract file whitespace checks pass. Tracked SetupWizard bin/obj outputs
remain unchanged. All invoked test/build sessions terminated.

## 2026-10-03: Captured Detail Requests And Lifetime

### Authoritative Changes

- Added optional IFormsDetailSynchronization and typed per-detail outcomes without
  adding mandatory members to IUnitofWorksManager. Details retain captured form/name
  identity, terminal state and evidence that legacy Get may have published. Empty/
  deferred/unattempted results cannot claim all details current. Legacy Task sync
  methods now propagate incomplete synchronization instead of silently allowing a
  dependent navigation/delete to proceed as though the refresh succeeded.
- Replaced shared-mode deferred forcing with a private captured request graph.
  Registered block/UoW targets, coordination and independent composite field pairs
  are copied before await. Forced requests select only that deferred subtree,
  leaving siblings/mode untouched. Explicit mapping lists now include every pair;
  invariant key values and root record identity are rechecked. Unreadable/absent
  runtime keys fail before clear rather than being confused with genuinely null keys.
- Traversals serialize within one manager, reject awaited reentrancy and participate
  in callback/preparation drain. Caller/lifetime cancellation reaches the populate
  trigger and safe queue/traversal boundaries. Close cancels queued work and waits
  for uncooperative Get acknowledgement before retiring private gate/source resources.
  No claim that legacy providers are forcibly stopped or stage their publication.
- Already-dirty targets/descendants block a branch before Get/Clear, without automatic
  save/discard. Independent clean siblings retain their own outcomes. Failed parents
  do not refresh descendants using old rows. Deferred markers clear only on acknowledged
  success; registration-bound suppression/marker updates cannot target a replacement.
- Pruned incoming/outgoing relationships after lookup retirement during unregister,
  including associated deferred markers and live role metadata. Cache/helper calls no
  longer execute inside that relationship ownership monitor. Bounded capture rejects
  cycles, more than 256 blocks or more than 128 levels before provider execution.

See [coordination and compatibility contracts](DETAIL-COORDINATION.md).
This is not a global mutation scheduler, staged read, arbitrary concurrent dirty-edit
protection, complete graph snapshot, typed navigation/delete side-effect contract,
LOV/validation generation or UI dispatcher qualification. Stages A-G remain open.

### Verified Results

Latest source-built Forms matrix: **412 passed per TFM**, zero failures/skips;
**1,236 executions** across net8.0/net9.0/net10.0. Log:
`%TEMP%/BeepDM-forms-detail-matrix-4.log`, terminal exit 0.
DetailCoordinationTests adds **32 cases per TFM** over the prior 380-case matrix:
targeted deferred mode/sibling ownership; queued A/B ordering; independent copied
mappings; dirty detail/descendant preservation; null-master immediate/deferred clear;
failed/null parent outcomes; sibling isolation; close/queue/caller cancellation;
reentrant/self-drain rejection; master/detail replacement and same-row key mutation;
composite culture-invariant filters; cycles/size limits; cancellation-aware triggers;
unregister graph pruning; empty no-op results; unreadable keys; exact suppression
identity and reentrant close before populate delivery. Barriers use bounded waits,
not sleeps to manufacture races.

FrameworkReliabilityTests: **1,393 passed per TFM**, zero failures/skips;
**4,179 executions**. Log:
`%TEMP%/BeepDM-forms-detail-framework-matrix.log`, terminal exit 0.
Combined final qualification: **5,415 executions**, zero failures/skips. Existing
warnings remain. No whole-solution, external provider/plugin, desktop adapter,
Unix or published-package consumer qualification is inferred.

Initial detail-baseline.log compiled: 373/380 passed, seven failures from old silent-
failure expectations or mocks returning null on successful reads. Revised denial
fixtures assert typed failure; successful read mocks return nonnull collections.
Baseline-2 passed 379/380; detail-1 passed 400/401. The remaining cascading-delete
fixture created a new CurrentItem object on every getter; fixed it to retain one
record until delete advances, and to acknowledge its read. Detail-2 passed 401/401.
The first matrix failed compilation on xUnit's obsolete Assert.Throws(Func<Task>)
overload; switched the self-drain assertion to an Action because that method throws
synchronously. Matrix-2 passed 408 per TFM. Matrix-3 passed 411/412 per TFM; its new
reentrant-close fixture incorrectly counted normal trigger-notification detachment
as populate delivery. A focused source-built-binary rerun identified that exact
assertion; the fixture now verifies no FireBlockTriggerAsync call. Matrix-4 above
is the final source-built result. Logs use `%TEMP%/BeepDM-forms-` prefixes.

### Skills And Remaining Gates

Updated forms, forms-helper-managers and forms-operations-navigation entrypoints in
repository/direct installed Codex folders. Replaced the navigation reference's
generic-UoW direct registration/automatic rollback example with source-backed
typed deferred/close fragments and explicit durability/publication limits. Preserved
automatic invocation and repository-only integration notes. Four reference pairs
match; all eight folders pass quick_validate.py in UTF-8 mode and their entrypoint
source paths resolve. Independent duplicate skill roots remain unqualified.

The example projection includes the new navigation fragments against actual Engine/
Models for net8/net9/net10; this is API compilation, not a UI session. Result below.
Current contract/plan/log links checked: 59, zero missing targets. Focused tracked-
file diff checks and five new source/test/model/contract whitespace checks pass.
Tracked SetupWizard bin/obj outputs remain unchanged. No commit, PR, stage closure
or goal completion is made by this increment.

C/D still require broader public-operation admission/cancellation, staged reads,
cross-operation/record generations, concurrent dirty edits and typed mutation
side effects. A/B and E/F/G retain their documented gates. The objective remains
active rather than being redefined around this tested detail subset.

Example build: `%TEMP%/BeepDM-forms-detail-examples-build.log`, terminal exit 0,
zero warnings/errors across all three TFMs. All test/build/validation sessions
invoked in this increment reached terminal state. Final focused diff checks pass.

## Staged Default Detail Reads (2026-10-03)

Added optional IStagedUnitofWorkRead/IUnitofWorkReadStage to Models, implemented
by the default datasource UnitofWork and explicitly forwarded by UnitOfWorkWrapper.
Prepare leaves prior collection/cursor/tracking live; accept swaps owned pointers
once; abort disposes only private candidates. The stage retains admission until
disposed, rejecting overlapping read/commit and replacement/clear routes. It checks
observed edit/cursor revision, dirty state, source/entity/tenant identity, disposal
and cancellation before acceptance. Typed rows have independent shells/Entity
observers, not deep-isolated nested graphs. Mapping failures/null result/row reject.

Default SecurityManager now implements optional IQuerySecurityPublication. Detail
refresh combines policy revision and registration retirement gates with owned-memory
UoW publication, never a provider/trigger/observer under those monitors. Custom
security helpers without that capability reject the staged path before provider
execution. No fallback occurs after staging fails. RecordsPublished and separate
NotificationFailures distinguish accepted rows from observer failure; PostQuery
cannot undo publication. ProviderMayHavePublished also remains evidence of legacy
Get invocation, not database durability or a no-effects/replay guarantee.

Preparation awaits physical IDataSource.GetEntityAsync acknowledgement; the legacy
provider API has no token overload. Close/caller cancellation discards unpublished
candidates only after that acknowledgement; async close/drain does not falsely
finish while provider work remains active. Borrowed UoWs/providers/old collections
remain undisposed. See [read contract](READ-PUBLICATION.md).

### Verified Results

Forms matrix: **456 passed per TFM**, zero failures/skips, **1,368 executions**
across net8.0/net9.0/net10.0. Log:
`%TEMP%/BeepDM-forms-staged-final-matrix.log`, terminal exit 0.
[StagedReadTests](../Forms.Tests/StagedReadTests.cs) adds **44 cases per TFM** to
the prior 412: actual UoW/wrapper prepare/abort/accept, cursor/tracking preservation,
dirty and observed change rejection, final gate cancellation/disposal, single-use/
expired/cross-thread action rejection, observer isolation, ownership/handler wiring,
overlap admission, bad mapping/provider/null/cancel inputs, tenant/copy boundaries,
legacy override/list-mode opt-in, stale master/policy/unregister/rebind/dirty detail,
physical close drain, queued new-master ordering, custom security gate rejection
and a framework-built/bound SQLite ADO.NET read. Barriers use bounded waits, not
sleeps to manufacture races.

FrameworkReliabilityTests: **1,393 passed per TFM**, zero failures/skips,
**4,179 executions**. Log:
`%TEMP%/BeepDM-forms-staged-framework-matrix.log`, terminal exit 0.
Combined: **5,547 executions**, zero failures/skips. Existing warnings remain;
no whole-solution, external plugin/provider, desktop adapter, Unix or published-
package consumer qualification follows. Skill example API compilation succeeds
across all three TFMs with zero warnings/errors:
`%TEMP%/BeepDM-forms-staged-examples-build.log`, terminal exit 0.

Initial staged-tests.log failed compilation because target-typed new() made Get
ambiguous with Get(int); the fixture now explicitly creates List<AppFilter>.
Staged-tests-2 passed 35/36: its prior-row array selected OBL's IList constructor,
which retains a fixed-size list, so legacy Clear could not clear it. The fixture
now uses List<Row>; staged-tests-3 passed 36/36. Staged-tests-4 passed 42/43 because
the retry fixture completed one barrier twice; it now resets the provider before
retry. Intermediate staged-matrix passed 455 per TFM; the final matrix above adds
the same-thread publication conformance test. These are fixture corrections,
not qualification of fixed-size legacy collections or universal provider behavior.

### Skills And Remaining Gates

Updated four Forms entrypoints/references in repository and direct installed Codex
folders, preserving automatic invocation and repository-only integration notes.
All eight folders pass quick_validate.py in UTF-8 mode; four reference pairs match.
Independent duplicate skill roots are still unqualified.

Basic/enhanced queries still self-publish and mode entry may pre-clear buffers.
Legacy/custom detail UoWs, clear semantics, transitive metadata/record graphs,
master changes in the final publication window, arbitrary concurrent edits,
cross-operation scheduling/generations, validation/LOV targets and UI host/dispatcher
conformance remain open. No stage or persistent goal is closed by this increment.
The scope remains FormsManager plus platform-neutral UI integration contracts.

Final checks: 73 contract/plan/log Markdown links and 60 entrypoint source references
resolve. Focused diff and new source/test/contract whitespace checks pass. Tracked
SetupWizard bin/obj outputs remain unchanged. All test/build/validation sessions
invoked in this increment reached terminal state. No commit or PR was created.

## Managed Query Publication Increment (2026-10-03)

### Implemented Behavior

Basic, enhanced and optional typed queries now share FormsManager.QueryExecution.cs.
Default datasource UoWs/wrappers stage reads, preserving implicit records/cursor/
mode until acceptance. Explicit ENTER_QUERY clearing and triggers remain unchanged.
Dirty captured targets/details block reads without automatic save/discard. Query/
detail reads share a manager-local ordering gate and reject awaited nested reads.
Newer same-registration requests supersede older unpublished candidates, even if
the newer request subsequently cancels. FormInstanceId, canonical BlockName,
RegistrationId and RequestRevision distinguish request identity across replacements.

IFormsQueryOutcomes exposes a token-aware query and FormQueryResult with typed
state, staging/publication evidence, legacy uncertainty and observer diagnostics.
Pre-cancelled/post-close calls reject before admission; admitted cancellation waits
for physical provider acknowledgement before returning an outcome. Accepted staged
rows retain Completed/RecordsPublished through later cancellation or observer error.
Managed queries and the mode-transition convenience wrapper join callback lifetime
drain. The wrapper retains query evidence through validation/navigation warnings.
Default publication combines security/registration gates, owned row acceptance and
CRUD mode; custom stages must acknowledge publication. No staged failure falls
back to legacy Get. A legacy null result is failed/unknown, not successful empty data.

### Verified Results

Forms: **496 passed per TFM**, zero failures/skips, **1,488 executions** across
net8.0/net9.0/net10.0. Log: `%TEMP%/BeepDM-forms-query-publication-matrix.log`,
terminal exit 0. QueryPublicationTests adds **40 cases per TFM** to the prior 456:
actual UoW/wrapper routes, implicit mode preservation, explicit entry compatibility,
policy/mode/registration rejection, copied queued filters, supersession, identity,
dirty descendants, shared ordering, linked cancellation/close drain, final security
gates, unsupported/custom capability failure, observer isolation, wrapper evidence
and framework-built/bound SQLite queries. Controlled barriers use bounded waits.

FrameworkReliabilityTests: **1,393 passed per TFM**, zero failures/skips,
**4,179 executions**. Log: `%TEMP%/BeepDM-forms-query-publication-framework.log`,
terminal exit 0. Combined: **5,667 executions**, zero failures/skips. This is
source-built Windows qualification, not whole-solution, published-package consumer,
external provider/plugin, Unix or real desktop adapter qualification.

Updated skill fragments compile against the actual Engine project for all three
TFMs: zero warnings/errors, `%TEMP%/BeepDM-forms-query-publication-examples.log`,
terminal exit 0. Existing test-build warnings remain. Initial net8 Forms execution
was 450/456 because six legacy Get mocks returned null. Shared fixtures now return
non-null empty results, preserving their original behavior assertions. New-test
compilation initially failed xUnit2014 for the self-drain assertion; a void-bodied
assertion preserves the synchronous rejection check. The next run passed 487/489;
two local MaxRecords fixtures also needed non-null Get returns. The following run
passed 495/495, and the final matrix adds the replacement-registration identity case.
These are fixture corrections, not relaxation of null-result failure semantics.

### Skills, Checks And Remaining Gates

Four Forms skill entrypoints/references and their direct installed Codex copies
now describe typed query publication, cancellation, ordering and lifetime contracts.
All eight folders pass UTF-8 quick_validate.py; four reference pairs are identical.
Implicit invocation remains enabled and repository-only integration notes remain.
Other duplicate skill roots are not qualified. Checks resolve 79 contract/plan/log
file links and 62 entrypoint source references. Focused tracked diff/new-source
whitespace checks pass; tracked SetupWizard bin/obj outputs remain unchanged.
All test/build/validation sessions invoked in this increment reached terminal state.
No commit or PR was created.

Legacy/custom UoWs can still self-publish/pre-clear; arbitrary concurrent editing,
mutable metadata/record graphs, final-window master changes, scalar/commit/navigation
admission, validation/LOV target generations and host/dispatcher conformance remain
open. Query/detail ordering is not a global operation scheduler. No full stage or
persistent goal is closed. Scope remains FormsManager and UI integration contracts.

## Captured Validation And LOV Increment (2026-10-03)

### Implemented Behavior

FormsManager.RecordTargets.cs captures registration/block/UoW, record/collection,
mode/query revision, registered item identities, declared field values and request
identity. Manager validation shares an annotation revision so nested field passes
supersede older record results even without data changes. Default UoWs expose
IUnitofWorkRecordRevision: canonical cursor/item/structural changes advance before
callbacks, without counting a second audit delivery of the original edit. Wrappers
forward it and retain support identity after disposal; unavailable supported sources
fail closed rather than falling back. This is an observed revision, not an edit lock.

ValidateField/ValidateBlock and manager-owned typed-value validation now recheck
captured targets around callbacks/helpers and before error annotation. Record-level
validation pins every captured item; replaced same-record items cannot receive old
results. Dictionary validation input uses a copied shell. Noncurrent row edits keep
the block change feed but do not annotate the current item's error store. Typed-value
LOV work uses linked close cancellation and acknowledges the helper before finishing.

IFormsLovOutcomes/ShowLOVWithOutcomeAsync returns FormLovResult; ShowLOVAsync projects
that result without retargeting a later CurrentItem. Return mappings and selected
values are copied before trigger/load, definition/target identity is rechecked, and
each setter uses the captured record. Own-write acknowledgement only advances by
expected canonical events: nested or independent-context ABA stops subsequent fields.
AppliedFields and SelectionEffectsPossible retain partial/uncertain setter evidence;
selection is not atomic and is not automatically rolled back or replayed. Trigger
failure/timeout/exception and invalid mappings reject before load. Nested awaited
LOV operations and self-drain reject. Admitted cancellation/close waits for physical
helper acknowledgement. Cache/raw helper events are not undone by late rejection.
Public LOV and synchronous manager validation join lifetime accounting/drain.

### Verified Results

Forms: **541 passed per TFM**, zero failures/skips, **1,623 executions** across
net8.0/net9.0/net10.0. Log: `%TEMP%/BeepDM-record-target-final-2-matrix.log`,
terminal exit 0. RecordTargetTests adds **45 cases per TFM** to the prior 496:
real UoW/wrapper and default LOV helper healthy/rejected selections, cursor/edit ABA,
repeated edits, captured values, query/mode/registration/item/definition changes,
nested validation, independent-context movement, close/cancellation acknowledgement,
null/failed/throwing loads, failed triggers, nested LOV, disposed revision source,
noncurrent change feed and partial setter evidence. Races use explicit bounded
barriers, not sleeps to manufacture order.

Framework build/run at `%TEMP%/BeepDM-record-target-final-2-framework.log` completed
but failed **2 child-process ownership cases per TFM** (1,391/1,393 passed each).
The unchanged two cases passed an isolated net8 diagnostic rerun:
`%TEMP%/BeepDM-record-target-child-diagnostic.log`. The full unchanged three-TFM
recheck passed **1,393 per TFM**, **4,179 executions**, zero failures/skips:
`%TEMP%/BeepDM-record-target-framework-recheck.log`, terminal exit 0. Recheck used
the existing source-built artifacts; Engine/Models hashes match current output for
all three TFMs. The intermittent child-process failure cause is not proven or
resolved by passing a rerun; retain it as a test-stability/release gate.
Combined final green Forms/framework executions: **5,802**, zero failures/skips.
This is Windows source-build evidence, not whole-solution, published-package,
external provider/plugin, Unix or real desktop adapter qualification.

Initial existing Forms run was 490/496: six mocks raised ItemChanged for an object
other than CurrentItem. Fixtures now use the actual current record without weakening
their original trigger/error/status assertions. The first new-test run was 499/524:
its real-list fixture had no selected cursor; it now explicitly MoveFirst before
registration. The next focused run passed 28/28. A later depth-guard edit initially
placed finally before catch, causing compile failure; its order was corrected.
Next run was 536/537: a disposed wrapper lost capability identity and incorrectly
permitted fallback. Cached support identity fixes that production bug. Subsequent
matrices passed 537 and 540 per TFM, then the final matrix above includes record-
validation replacement-item coverage. No failure assertions were relaxed.

### Skills, Checks And Remaining Gates

Four Forms entrypoints/references and their direct installed Codex copies now route
to RECORD-TARGETS.md and describe typed LOV partial-selection evidence. All eight
folders pass UTF-8 quick_validate.py; four reference pairs match. Implicit invocation
and repository-only integration notes remain. Duplicate skill roots remain unqualified.
Updated LOV/example fragments compile against the actual Engine project for all three
TFMs with zero warnings/errors: `%TEMP%/BeepDM-record-target-examples.log`, exit 0.
Existing test-build warnings remain. Checks resolve 89 contract/plan/log file links
and 68 entrypoint source references. Focused diff/new-source whitespace checks pass;
tracked SetupWizard bin/obj outputs remain unchanged. All invoked sessions reached
terminal state. No commit or PR was created.

Raw helper events/cache, lookup policy, async rule execution/context isolation,
mutable metadata/definition/record graphs, opaque nested values, unobserved edits,
final check-to-setter concurrency, editor completion and host/dispatcher generations
remain open. This increment is not a global edit/commit/navigation scheduler. No
full reliability stage or persistent goal is closed; scope remains FormsManager and
platform-neutral UI integration contracts.

## Captured Editor Popup Increment (2026-10-03)

### Implementation And Compatibility

Editor requests now use captured record/registration/item/query/mode identity,
observed UoW revisions and per-field request supersession. Named definitions are
checked and copied for the borrowed provider. Current block/item insert/update
permission, disabled/hidden/read-only/criteria state and field security are checked
before raw text disclosure and before applying the result. Tracking identifies
new rows in CRUD mode. Masked fields reject raw-text editors even for admins.

Optional IFormsEditorOutcomes/FormEditorResult separates provider invocation,
acknowledgement and OK from setter attempt, setter acknowledgement and accepted
completion. Legacy ShowEditorAsync projects OK only on Completed; failed/denied/
superseded completion projects cancellation. Pre-cancelled/post-close admission
throws; admitted provider faults are typed failures, not legacy escaping exceptions.
Partial effects remain possible after a setter reenters close/navigation. Rejected
completion withholds text; neither callback drain nor typed failure rolls back writes.
See [current record contract](RECORD-TARGETS.md).

Editor calls link caller/close cancellation and join physical callback drain.
Borrowed IEditorProvider owns UI dispatch, dismissal and acknowledgement. A manual
dispatch-queue fixture verifies that close does not claim completion while a dialog
callback remains queued. Awaited nested editors/self-drain reject. This is an initial
Stage E provider-boundary increment, not full host/view/presenter conformance.

### Source-Built Qualification

Initial net8 run passed 566/566. The final matrix passed **571 Forms tests per TFM**,
**1,713 executions** on net8/net9/net10, zero failures/skips:
`%TEMP%/BeepDM-editor-final-matrix.log`, terminal exit 0. Thirty new cases use the
real default UoW/wrapper and cover healthy/copy-safe input, cursor/edit ABA, item/
definition/configuration change, retirement, permissions/raw disclosure, new-row
insert permissions, setter rejection, late cancellation/close, newer requests,
null/fault provider, nested work and effects retained after close inside a setter.
Ordering uses provider barriers/manual dispatch, not sleeps.

Framework source build/run passed **1,393 cases per TFM**, **4,179 executions**:
`%TEMP%/BeepDM-editor-framework.log`, terminal exit 0. The unchanged prior child-
process instability remains an unresolved release gate, not resolved by this pass.
The final Forms matrix includes a subsequent defensive text-clear in the rejection
catch. Its framework source-build run (`%TEMP%/BeepDM-editor-final-framework.log`,
exit 1) passed net8/net9 but failed the net10 lifecycle alias-removal case
PendingCreationRemovedByEitherAlias_CannotPublishAnOldGeneration(useGuid: True):
1,392/1,393. Quiet output recorded the case, not its exception details. This is
distinct from the earlier child-owner failures; its cause is not proven. No source
or assertions changed for the diagnostic/recheck. Isolated net10 alias cases passed
2/2 (`%TEMP%/BeepDM-editor-lifecycle-diagnostic.log`, exit 0). The unchanged full
three-TFM normal-verbosity recheck passed 1,393 per TFM, 4,179 executions
(`%TEMP%/BeepDM-editor-framework-recheck.log`, exit 0). All six Engine/Models test
assembly hashes match current output. Final green Forms/framework count: **5,892**
executions, zero failures/skips. Retain both preceding failure records as unresolved
test-stability/release evidence; passing reruns do not repair them.

Four Forms skill entrypoints/references and direct installed copies now describe
typed editor outcomes, effect evidence and borrowed provider dispatch. All eight
folders pass UTF-8 quick_validate.py; four reference pairs match byte-for-byte.
Entrypoints intentionally retain repository-only integration differences; an initial
overbroad entrypoint hash comparison rejected that expected difference, not skill
validation. Popup/example fragments compile against actual Engine references on
all three TFMs: `%TEMP%/BeepDM-editor-final-examples.log`, zero warnings/errors, exit 0.
Existing source/test warnings remain. Contract/plan/log file links and source-path
checks resolve; focused git diff and new-source whitespace checks pass. Tracked
bin/obj outputs remain unchanged; existing untracked persistence-worker outputs
are preserved. All invoked sessions reached terminal state. No commit or PR was made.

### Remaining Gates

No full stage is closed. Host/view/presenter binding/focus/error generations and
real adapters, mutable policy/definition ABA, final check-to-setter concurrency,
opaque nested/unobserved edits, auxiliary lookup policy/cache and async rule/context
isolation remain open. No parallel host API or desktop-control redesign was added.
The full plan remains active; this increment does not narrow the goal to editor work.

## Opt-In Host Binding Increment (2026-10-03)

### Implementation And Compatibility

[UI binding contracts and E-01 through E-09 checklist](UI-BINDING-CONTRACTS.md)
describe the opt-in FormsViewBinding utility over existing IBeepFormsHost/IBlockView/
IFieldPresenter, not a parallel host or automatic desktop adapter migration. Optional
IFormsDispatcher and IOriginAwareFieldPresenter model ordered physical dispatch and
programmatic origin/revision. IFormsBindingTargets issues immutable public identity
and owns captured record/item/mode/query/observed revision and policy evidence.
Capturing another token does not supersede prior current tokens. ApplyBindingValue
clones evidence, reuses captured writes/permissions and invalidates old tokens.
Synchronous capability calls join manager lifetime accounting.

Binding owns only its view binding/listeners. It checks manager/registration/record,
view-state and presenter roster before render/edit/focus; retirement/close rejects
queued work. It isolates subscription cleanup failures, distinguishes attempted from
acknowledged effects, and physically drains running/queued dispatch. Delivery and
detach share duplicate/late/missing acknowledgement fences. Failed cleanup does not
claim Unbind occurred; UI-context repair remains possible. Self-drain rejects.

Rendering preserves nullable values, copies byte buffers, applies field/block flags
and current item errors, masks without raw fallback and clears a failed mask's value
when its target remains current. Default field policy setters now clone input,
increment SecurityRevision and return copies from getters. Use SetFieldSecurity to
publish edits; mutating a GetFieldSecurity result no longer changes active policy.
Binding rejects configured security helpers without snapshot revision support.

Legacy presenters suppress only nested inline echoes per presenter. Deferred echoes
need the optional origin protocol; retired programmatic stamps never reenter writes.
Actual user events remain separate, not globally suppressed. The helper consumes
selected existing UnitOfWorkActivity relays; ItemChanged does not duplicate the
manager field feed. PostCommit means actual UoW completion after tracking acceptance,
not a relabeled collection AfterSave. Failed UI notification is distinct from saved
tracking/durability; it does not replay a commit or mark the buffer dirty again.

### Source-Built Qualification

Final Forms matrix passed **617 tests per TFM**, **1,851 executions** on net8/net9/
net10, zero failures/skips: `%TEMP%/BeepDM-binding-final-6-matrix.log`, exit 0.
Forty-six added HostBehaviorTests cases use actual default UoWs/wrappers plus
existing host/view/presenter interfaces and a recording manual dispatcher. Cases
cover worker affinity/order, nullable edits, record/registration/policy/view-state/
roster retirement, cursor/edit/context ABA, field policy copies, permission flags,
captured focus/refusal/cancellation, inline/unrelated/deferred/retired echoes, mask
null/failure, byte copying, plain/dictionary legacy records, own subscription unwind,
failed remove/cleanup repair, and duplicate/late/early physical acknowledgement.
The real commit fixture proves provider acknowledgement/tracking acceptance survives
a failed UI notification without returning to unsaved display state. It is not proof
of a particular native host implementation. Ordering uses barriers/manual dispatch,
not sleeps; all early-action worker tasks are awaited to terminal acknowledgement.

Before HostBehaviorTests, the net8 existing suite passed 571/571. Its first new-test
build failed for a missing PassedArgs namespace import, then passed 597/597. Expanded
610-per-TFM runs failed one field-policy callback case per TFM: default SetFieldSecurity
had not advanced the revision and exposed mutable policy aliases. The production
setter/getter ownership/revision fix retains that regression assertion. Expanded
612/614 runs failed the post-commit UI-state case: the fixture relabeled AfterSave
before the collection's final reset. It now relays actual UoW PostCommit, preserving
all durability and dirty-display assertions. Later 614/615/616 matrices passed;
the final 617 includes guarded detach acknowledgement/repair. Existing assertions
were not weakened. Final changed source/test files introduce no observed warnings;
repository-wide pre-existing build/test warnings remain.

Initial and final framework source-build matrices each passed **1,393 per TFM**,
**4,179 executions**, zero failures/skips:
`%TEMP%/BeepDM-binding-framework.log` and `%TEMP%/BeepDM-binding-final-framework.log`,
both terminal exit 0. Combined latest green Forms/framework: **6,030 executions**.
Earlier intermittent child-owner/lifecycle failures remain unresolved release
evidence; passing this increment does not repair their cause.

### Documentation, Skills And Remaining Gates

Plans/trackers and four Forms skill entrypoints/references/direct installed copies
route to the new adapter contract/checklist and preserve implicit invocation and
repository-only integration notes. All eight skill folders pass UTF-8 validation;
four reference pairs match. The opt-in binding/focus fragment is compiled with the
existing examples against actual Engine references on all three TFMs:
`%TEMP%/BeepDM-binding-final-examples.log`, zero warnings/errors, exit 0.
Checks resolve 105 contract/plan/log file links and 78 skill source references.
Focused git diff and all new-source/document whitespace checks pass. Six final
framework Engine/Models assembly hashes match current source-built output.
Tracked bin/obj files remain unchanged; existing untracked persistence-worker
outputs are preserved. All invoked sessions reached terminal state; no commit or PR.

No stage or full goal is closed. Real host routing, native adapters, custom edit/
query-criteria/trigger/focus order, async user/error provenance, automatic policy/
error-completion repaint, cached rows under changed principal/row filters, all-public
scheduling/lifetime, opaque nested data, metadata/graph ownership, final-window edits
and auxiliary provider/cache policy remain gates. Fresh UI tokens are not evidence
that a held buffer has been re-authorized for a new tenant. Full scope remains
FormsManager and platform-neutral UI integration contracts, not control redesign.

## Cached Buffer Authorization Increment (2026-10-03)

### Implemented Boundary

[BUFFER-AUTHORIZATION.md](BUFFER-AUTHORIZATION.md) documents new optional UoW/stage
buffer identities, default read-policy revisions and registration-scoped publication
receipts. Accepted managed basic/enhanced/typed query and detail publication install
the receipt before observers, inside the existing owned-memory acceptance window.
Fresh UI target capture checks it before record fields; existing record-aware targets
also require it. A new principal/roles/claims/block-row policy cannot bless a held
old buffer simply by capturing a new token or re-registering it. Field-only policies
expire old delivery tokens but allow fresh remasking without another provider read.

Default UoW/wrapper identity survives cursor/field/commit tracking changes and
invalidates on replacement, Clear, filtered-buffer assignment and ScopeToTenant.
Staged acceptance checks the captured buffer identity as well, rejecting a tenant
scope ABA during physical provider acknowledgement. Scoped preloaded registration
cannot use the narrow trusted unscoped compatibility receipt. Disposed wrappers keep
support identity but cannot expose a live buffer. Receipts also pin provider,
entity/schema/default clause and helper/read-policy identity; no external getters
or callbacks were added inside publication monitors. Existing constructor and
mandatory host/UoW contracts remain compatible; stricter scoped attachment is
documented, not silently downgraded to legacy trust.

### Qualification And Checks

[HostBehaviorTests](../Forms.Tests/HostBehaviorTests.cs) adds **30 cases per TFM**.
Initial source-built Forms net8 run: 617 passed; initial new cases: 636 passed.
Expanded matrices passed 643, then 644 and 645 per TFM. Final matrix passed
**647 per TFM**, **1,941 executions**, zero failures/skips:
`%TEMP%/BeepDM-buffer-final-3-matrix.log`, terminal exit 0.
Tests preserve the existing 617 assertions and exercise no raw host getter after
rejection, managed query/detail restoration, field-only masked rendering, failed/
in-flight reads, tenant-scope ABA, scoped initial registration, buffer/provider/
schema/default-clause changes, re-registration and pre-observer receipt delivery.

Initial and final source-built framework matrices each passed **1,393 per TFM**,
**4,179 executions**, zero failures/skips:
`%TEMP%/BeepDM-buffer-framework.log` and `%TEMP%/BeepDM-buffer-final-framework.log`,
both terminal exit 0. Latest combined matrix: **6,120 executions**. Earlier
child-owner and net10 lifecycle intermittent failures remain unresolved release
evidence; this clean increment does not establish their cause or repair.

Four Forms skills and direct installed copies route to the new contract; references
match and all eight folders pass UTF-8 skill validation. The compiled example adds
managed-read acknowledgement before fresh binding capture. Its first compile
reported three CS8632 warnings because the new nullable return lacked an enabled
annotation context; the example now enables nullable annotations/checks. Final
example results and focused file/link checks are recorded below.

Final example project builds net8/net9/net10 with **zero warnings/errors**, exit 0:
`%TEMP%/BeepDM-buffer-final-examples.log`. Final validation resolves **133**
contract/plan/log links, limited to the current Forms section of the root tracker.
A broader initial root-tracker scan encountered its existing unrelated link to
`editor-managers/MASTER-EDITOR-MANAGERS-TRACKER.md`; historical non-Forms links
were not repaired or claimed validated. Focused tracked diff and new-file whitespace
checks pass; Git's existing LF/CRLF conversion notices are not whitespace failures.
All six framework Engine/Models hashes match current source-built Engine output.
Tracked bin/obj output remains unchanged; the four existing untracked persistence-
worker FileSystemGlobbing DLLs are preserved. All invoked process sessions reached
terminal state. No commit, PR, worktree or separate chat was created.

### Remaining Gates

No full stage or goal closes. Hosts still must hide/clear already displayed
previous-context text and request managed re-query/repaint; rejecting a token does
not erase controls. Automatic policy/error repaint, custom host routing and real
adapters, auxiliary cache/lookup/export/audit policy, arbitrary raw row additions/
mutations, opaque graphs and final-window concurrent setters remain unqualified.
This receipt is not a row-predicate interpreter or database authorization. Existing
transaction, all-public lifetime/scheduler, graph/metadata/configuration, paging
and release gates remain in the authoritative A-G plan.

## Policy Reconciliation Increment (2026-10-03)

### Implemented Boundary

[POLICY-REPAINT.md](POLICY-REPAINT.md) documents optional immutable policy revision
feeds, default helper/facade relay, callback ownership and the opt-in binding's
latest-policy UI pump. SecurityManager notifies outside its publication monitor,
isolates observers and retains 128 failures. FormsManager projects flags, relays
current revisions, owns only its subscriptions and records separate bounded
projection/observer failures. Four policy setters now reject post-close admission
and join physical callback acknowledgement/drain; default direct-helper mutations
are observed, while unobservable custom helper direct calls remain a host boundary.

One admission-counted pump defers inline dispatchers, coalesces revisions and checks
current owner/registration/view-state/roster/policy before UI delivery. It remasks
authorized current rows or hides/disables/read-only/clears revoked presenters and
known BeepViewState presentation metadata/history without raw host field getters,
SyncFromManager or UoW discard. Dirty state is preserved. Setter failures are
isolated across remaining owned controls, retaining aggregate partial-effect
evidence instead of claiming erasure. A newer generation stops old delivery and
queues latest reconciliation. Accepted queries while queued render the new buffer.
RequestPolicyReconciliationAsync provides deliberate typed reconciliation after a
dispatch failure without republishing policy or querying records; cancellation,
retirement and physical early-ack draining remain guarded. No automatic retry loop.

### Qualification

[HostBehaviorTests](../Forms.Tests/HostBehaviorTests.cs) adds **31 cases per TFM**
for automatic worker clearing/remasking, preserved dirty rows/metadata, burst and
reentrant policies, accepted-query timing, stale ownership, partial failures,
facade/direct/shared/legacy helper feeds, bounded diagnostics, explicit reconcile/
cancellation and physical callback/early-dispatch drain. Source-built final Forms
matrix: **678 per TFM**, **2,034 executions**, zero failures/skips, terminal exit 0:
`%TEMP%/BeepDM-policy-final-3-matrix.log`.

Initial 647 net8 run failed one stale-render case's old presentation-count
assertion: the stale render still stopped, but automatic remasking added a new
accepted presentation. The fixture now asserts the exact sequence first/*****
and unchanged/clean underlying row, retaining the original Superseded assertion.
669 net8 cases then passed. The expanded three-TFM compile selected xUnit's obsolete
Func<Task> Throws overload for a synchronous self-drain assertion; corrected it to
an Action that verifies rejection before a Task is returned. The 675/677 matrices
passed, then final 678 added physical early-ack pump proof. No runtime regression
was hidden by removing the stale-render or durability assertions.

Initial/final source-built framework matrices each passed **1,393 per TFM**,
**4,179 executions**, zero failures/skips and terminal exit 0:
`%TEMP%/BeepDM-policy-framework.log` and `%TEMP%/BeepDM-policy-final-framework.log`.
Latest combined Forms/framework result: **6,213 executions**. Earlier child-owner
and net10 lifecycle intermittent failures remain release gates; this clean run
does not establish or repair their cause.

Four Forms skills and direct installed copies now route to policy reconciliation
and E-10, preserving implicit invocation and repository integration notes. All
eight UTF-8 validators pass and four reference pairs match. The source-referenced
example adds hidden-surface principal switch, policy drain, accepted query and
render acknowledgement; final compile/link/hash/output checks follow below.

Final source-referenced example builds net8/net9/net10 with **zero warnings/errors**,
terminal exit 0: `%TEMP%/BeepDM-policy-examples.log`. **146** contract/plan/log file
links resolve, limited to the root tracker's current Forms section; unrelated
historical links remain outside this verification. All six framework Engine/Models
hashes match current source-built Engine outputs. Focused tracked/new-file whitespace
checks pass; unchanged Git LF/CRLF notices are not whitespace failures. No warnings
were observed for changed policy source/test files in the final Forms build log;
repository-wide pre-existing warnings remain. Tracked bin/obj files are unchanged,
and the four existing untracked persistence-worker FileSystemGlobbing DLLs remain
preserved. All invoked process sessions and the early-action worker reached terminal
state; no commit, PR, worktree or separate chat was created.

### Remaining Gates

No full stage or goal closes. Queued clearing is not instantaneous or atomic;
hide/lock before principal switch for immediate privacy, await physical drain,
inspect failures and keep the surface hidden until accepted current query/render.
Custom/native adapters must qualify E-01 through E-10. Arbitrary view metadata,
trusted queued message payloads, async error/user-event provenance, unobserved raw
model/helper changes, policy flag reset and explicit user/configuration ownership,
auxiliary caches/lookup/export/audit policy and endless command cycles remain open.
All prior A-G transaction, broad scheduler/lifetime, graph/metadata, paging and
release gates remain intact. No WinForms/WPF control redesign or new parallel host.

## 2026-10-03: Permission Projection Increment

Progress toward the full A-G plan; no complete stage or goal closes. Scope remains
FormsManager and platform-neutral UI integration, not native controls.
[Permission Projection](PERMISSION-PROJECTION.md) is the current contract.

### Authoritative Changes

Block query/CRUD and item Enabled/Visible setters now retain authored configuration;
getters combine it with an immutable registration-owned, revision-fenced overlay.
Same-value writes under denial configure restrictions without lifting policy.
Clone carries authored item flags; serialization still reports effective public
flags and is not authored-form persistence. Models has no new Newtonsoft dependency.

Manager projection captures all live registrations, restoring policy grants when
rules are removed, and applies preinstalled policy before BlockEnter observers.
Default optional IItemSecurityProjection checks exact item registries and fences
synchronous same-thread publication. Lock order is helper registry -> security
revision -> manager registration. Effective-change observers run outside monitors,
are isolated, and stop on newer policy/registration or item-registry replacement.
Legacy injected helpers have documented weaker publication/notification guarantees;
their individual evaluation APIs, not bulk callbacks, are authoritative.

### Qualification

Initial net8 build failed because proposed public configuration getters used
Newtonsoft attributes in the dependency-free Models project. Keeping configuration
getters internal removed the dependency and accidental serializer exposure; the
existing 678 net8 Forms tests then passed unchanged.

New tests initially lacked the security Models namespace and Moq extension import;
another fixture assumed the wrong EventManager namespace/IDisposable capability.
These compile failures were corrected without runtime workarounds. Three initial
runtime fixture failures were corrected: the no-filter mocked read now has a
setup and exact call-count assertion; the serializer fixture explicitly handles
existing System.Type metadata; the UI fixture explicitly refreshes after accepted
read. The subsequent empty filtered-read assertion was replaced with the exact
no-filter call assertion, retaining denial-before-provider evidence.

The first 701-case multi-TFM run failed the foreign-thread test on each TFM because
waiting on Task.Run can inline work on the calling thread. A dedicated joined
thread now proves cross-thread rejection, with no timing sleeps. The unchanged
runtime passed all 701 cases on all TFMs. The BlockEnter test was then strengthened
to capture values in its callback and assert outside callback exception handling.

The preceding source-built Forms result: **701 passes per TFM**, net8/net9/net10, **2,103
executions**, zero failures/skips and terminal exit 0:
`%TEMP%/BeepDM-permission-final-matrix.log`. Includes **23 added cases** in
PermissionProjectionTests and HostBehaviorTests. Framework result: **1,393 passes
per TFM**, **4,179 executions**, zero failures/skips and terminal exit 0:
`%TEMP%/BeepDM-permission-framework.log`. Combined: **6,282 executions**.

Earlier intermittent child-process owner and net10 lifecycle alias-removal
failures remain unresolved release gates; this clean matrix does not repair or
establish their cause. Existing repository compiler/analyzer and SQLite package
advisory warnings remain; this is not a warning-free framework certification.
No warnings were reported against the new projection/model/test source files in
that Forms log.

The four repository Forms skills and direct installed Codex copies now route to
configuration/projection and cached-row limits, preserving integration footers and
implicit invocation. Eight UTF-8 validators pass; four reference pairs match.
The source-referenced examples include authored restriction under denial and build
all three TFMs with **zero warnings/errors**, terminal exit 0:
`%TEMP%/BeepDM-permission-examples.log`.

Final inspection found a failed projection action could be called again inside
the same authorization window. An attempted-action fence now consumes the action
before any model writes, even if publication throws; an additional regression
verifies the retry rejects and retains the prior denial. Final Forms matrix:
**702 passes per TFM**, **2,106 executions**, zero failures/skips, terminal exit 0:
`%TEMP%/BeepDM-permission-final-2-matrix.log`. This increment now adds **24 cases**.
Final framework requalification: **1,393 passes per TFM**, **4,179 executions**,
zero failures/skips and terminal exit 0:
`%TEMP%/BeepDM-permission-final-framework.log`. Final combined result: **6,285
executions**, net8/net9/net10. All prior intermittent release gates remain open.
Final source-referenced examples build net8/net9/net10 with **zero warnings/errors**,
terminal exit 0: `%TEMP%/BeepDM-permission-final-examples.log`. All six current
Engine/Models assembly hashes match framework test outputs. The expanded scoped
document check resolves **176** file links, including the Forms README; its two
stale Models/Configuration catalog links now point to the actual Models project.
The root tracker check remains limited to the current Forms section, not unrelated
historical links. Focused tracked-file whitespace and new projection-file trailing
whitespace checks pass. Tracked bin/obj files remain unchanged and the four existing
untracked persistence-worker FileSystemGlobbing DLLs are preserved. All invoked
process sessions reached terminal state; no commit, PR, worktree or separate chat
was created. Eight skill validators and four matching reference pairs remain valid.

### Remaining Gates

No atomic guarantee spans whole block/item/UI graphs or registration plus projection.
Partial model publication can precede failure. Full configuration pinning/persistence,
arbitrary concurrent edits, raw helper replacement feeds, property classes, custom
helper qualification, auxiliary read/cache/export/audit policy and real adapter
E-01 through E-10 remain open. Clearing policy still revokes old row authorization;
accepted managed query/detail and acknowledged render are required. Queued clearing
is not immediate privacy; hide/lock before principal switch. All earlier A-G
transaction, scheduler/lifetime, graph, paging and release gates remain intact.

## 2026-10-03: Local Paging Increment

This implements the local portion of BF-09/Stage F toward the full A-G objective,
not a narrower replacement objective. Bounded provider fetch/prefetch/cache remains
required and unimplemented. No full stage or goal closes; no native control redesign.
[Local Paging](LOCAL-PAGING.md) documents the current contract and compatibility.

### Authoritative Changes

Optional IFormsLocalPaging exposes typed cursor/page/effect/notification evidence.
LoadPageAsync now returns only acknowledged local page state. Default optional
ILocalPagingPublication prepares without publishing, invalidates proposals on
helper changes and consumes a failed publication attempt. Helper bookkeeping is
case-insensitive, distinguishes stored zero, and clamps after count shrink.
PageInfo adds exact long count/offset math; legacy int projections throw checked
overflow instead of wrapping. Size zero disables local paging.

Default local requests capture buffer authority, registration/request/query,
cursor/observed record revision, loaded count, mode, configuration and policy.
Queued newer requests supersede older unpublished ones; nested paging rejects
instead of self-waiting. Existing navigation receives optional page checkpoints
and cursor acknowledgement callbacks. Ignored setters/unreadable count cannot
invent a loaded page. Accepted page evidence precedes post-navigation observers;
their failures cannot undo acknowledgement. Caller/close cancellation joins
physical callback drain; page gate retirement follows that drain. General legacy
navigation keeps its existing behaviour and is not globally scheduled by this work.

Local paging cannot recertify older-context buffers, consume virtual/provider
pages or criteria buffers, or fall back to eager unsafe legacy helper writes.
Stored lazy/fetch-ahead/max-fetch settings are explicitly not bounded provider
fetching. Metadata cache eviction does not automatically query or erase UI rows.
Provider/trigger/getter/observer callbacks remain outside ownership monitors.

### Qualification

The first multi-file patch was structurally rejected before edits because it
attempted delete/add on the same path; the helper was then updated in place.
Initial net8 baseline passed all existing **702** Forms cases. The first **24**
local regressions passed net8 (**726** total); six additional cases cover strict
count metadata, accepted query shrink, nested admission, invalid requests and
precancelled/closed calls. All **732** cases passed on net8/net9/net10.

Final source-built Forms matrix after public capability comment alignment:
**732 passes per TFM**, **2,196 executions**, zero failures/skips, terminal exit 0:
`%TEMP%/BeepDM-local-paging-final-2-matrix.log`. The preceding unchanged matrices
also passed all 732 cases: `%TEMP%/BeepDM-local-paging-matrix.log` and
`%TEMP%/BeepDM-local-paging-final-matrix.log`. No warnings were reported against
new local paging/models/helper/test source files. Repository-wide existing
compiler/analyzer and SQLite package advisory warnings remain.

The initial broader source-built framework matrix passed **1,393 per TFM**,
**4,179 executions**, zero failures/skips and terminal exit 0:
`%TEMP%/BeepDM-local-paging-framework.log`. Final framework requalification also
passes **1,393 per TFM**, **4,179 executions**, no failures/skips, terminal exit 0:
`%TEMP%/BeepDM-local-paging-final-framework.log`. Combined final Forms/framework
result: **6,375 executions**, net8/net9/net10. All six source-built Engine/Models
assembly hashes match framework test outputs.
The earlier child-process owner and net10 lifecycle alias-removal intermittent
failures remain unresolved release gates; clean runs do not establish their cause.

Three repository/direct installed skill pairs changed: forms, navigation and
performance/configuration. Five current reference pairs match and all ten UTF-8
validators pass. The performance skill now uses actual Models configuration
paths and source-backed compiled local/configuration examples, not a form returned
over already disposed using-scoped UoWs. The other two maintained skill pairs
remain unchanged. Configuration persistence examples are compiled, not executed.
All source-referenced examples, including the new performance/configuration
fragment, build across all three TFMs with **zero warnings/errors**, terminal
exit 0: `%TEMP%/BeepDM-local-paging-examples.log`. **183** scoped document file
links resolve, with the root tracker limited to its current Forms section, not
unrelated historical links. Focused tracked-file and new-file whitespace checks
pass; LF/CRLF Git notices are not whitespace failures. Tracked bin/obj files remain
unchanged and the four existing untracked persistence-worker FileSystemGlobbing
DLLs are preserved. All invoked sessions reached terminal state. No commit, PR,
worktree or separate chat was created.

### Remaining Gates

Stage F still requires bounded staged provider fetch, deterministic unique
ordering, row/byte constraints, long-count requests, physical acknowledgement,
dirty-row protection and bounded policy/query/registration-aware prefetch/cache.
An existing paged overload or virtual callback does not meet those requirements.
Whole cross-operation scheduling, raw helper/definition observation, cross-form
shared-helper ownership, arbitrary concurrent edits and real UI adapters remain
unqualified. Triggers/dirty-save callbacks can have separate effects; cancellation
inside a cursor callback can retain a changed cursor with old page state. Preserve
typed evidence and reconcile, never blindly replay or roll back foreign effects.
All A-G transaction, metadata/graph, broad lifetime, auxiliary policy and release
gates remain intact. The full goal remains active.

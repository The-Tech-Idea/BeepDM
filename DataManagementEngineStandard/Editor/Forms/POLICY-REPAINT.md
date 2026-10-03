# Revision-Gated Policy Reconciliation

Status: initial Stage B/C/E implementation for the opt-in binding, not a real
adapter qualification, all-public scheduler or synchronous privacy barrier.

## Optional Feed And Ownership

[Optional policy contracts](../../../DataManagementModelsStandard/Editor/Forms/Interfaces/IFormsPolicyNotifications.cs)
carry only immutable full/read-policy revisions, not principal or record values.
The default [SecurityManager](Helpers/SecurityManager.cs) publishes outside its
policy monitor after context/block/clear/field changes. It isolates observers and
keeps the last 128 failures in PolicyNotificationFailures; an observer cannot undo
an accepted policy or prevent other observers from receiving it.

[FormsManager](FormsManager.Security.cs) subscribes to IObservableSecurityPolicy,
projects security flags, then relays current revision notifications through optional
IFormsPolicyNotifications. Projection and observer failures have separate bounded
evidence. Stale/reentrant older revisions stop delivery to subsequent observers.
SetSecurityContext/SetBlockSecurity/SetFieldSecurity/ClearBlockSecurity now reject
post-dispose admission and join callback draining. Admitted setters/notifications
drain through physical synchronous helper/observer acknowledgement, not logical
close alone. Self-drain rejects; borrowed helpers and other forms' listeners survive
exact unsubscription. Attempted attachment is recorded before add and unwound on failure.

For an injected helper without observable capability, facade setters retain flag
projection and emit a snapshot-based invalidation. Direct calls on such a helper
are not observed; expose the optional feed or explicitly refresh through the host.
Missing/negative policy revisions reject automatic or explicit reconciliation;
ordinary binding's snapshot/receipt requirements remain independent.
Context identity/roles remain trusted application inputs, not verified authentication.

## Latest-Policy UI Pump

[FormsViewBinding](Helpers/FormsViewBinding.cs) owns one optional manager listener.
One admission-counted asynchronous pump coalesces newer revisions, including policy
changes made inside presenter callbacks. It defers inline dispatchers to avoid
recursive UI delivery. No UI getters/setters run in the policy publisher, and no
provider, presenter or observer runs under its ownership monitor.

At dispatch, the action checks current manager/view/view-state/presenter roster,
registration lifetime and policy revision. A policy invalidation is not an edit or
focus completion: it may capture the current authorized buffer at dispatch, including
a managed query accepted while queued. Edits/focus retain their originating token.
Old policy work cannot clear a new owner, roster or newer context.

An authorized buffer is remasked/refreshed through the existing render boundary.
Field-only changes need no provider read. A revoked [buffer receipt](BUFFER-AUTHORIZATION.md)
causes privacy clearing instead: hide/disable/read-only each owned presenter, set
its value and validation error to null, then clear known BeepViewState presentation
strings, message/severity/error/active-item and workflow-history data. Never call
SyncFromManager or raw host field getters in that rejection branch, and never Clear,
save, discard or mutate UoW records. Dirty/query/bootstrap/block identity state is
not falsified. BeepViewState identity is pinned even without a notification service;
arbitrary custom state remains adapter-owned.

Each external clear setter is guarded and isolated: one failure does not prevent
attempts on the other still-owned fields/state. Failed clearing retains aggregate
exception and EffectsPossible, without claiming Acknowledged. A changed generation
stops the old action; the owned pump then reconciles the newest publication. Pump
admission is included in WaitForPendingDeliveriesAsync/DisposeAsync through actual
dispatcher acknowledgement. UI failure does not undo policy or row durability.
RequestPolicyReconciliationAsync allows a deliberate revision-checked clear/remask
after a dispatch failure without republishing the policy or querying records. Inspect
its result and possible partial effects; it is not an automatic blind retry loop.

## Adapter And Remaining Gates

Clearing is queued, not instantaneous. Until dispatcher acknowledgement, previously
displayed text can remain visible. Hosts requiring immediate non-disclosure must
hide/lock the surface before switching principal, then await binding drain and
inspect failures. A refused setter/dispatcher is not proof that text was erased.
Do not block a UI context needed by the dispatcher while draining it.

The binding does not automatically query/retry/discard dirty data. After a revoked
buffer, run an accepted managed query/detail read and relay accepted PostQuery or
request refresh. A query accepted before queued reconciliation can be rendered
directly. It does not qualify custom host pipelines, native dispatchers, arbitrary
custom view metadata, already queued trusted message payloads, asynchronous error
completion repaint, raw helper mutation without a feed or every row/cache/export
policy. Endless reentrant policy publications are not a global command-cycle guard.
The [permission projection increment](PERMISSION-PROJECTION.md) restores policy
grants after rule removal without lifting authored restrictions. Default projection
is registry/registration/revision gated; whole-configuration snapshots, raw item
replacement feeds and injected-helper qualification remain open. Rule removal
still revokes cached-buffer authorization and does not automatically re-query.
DefaultWhere/provider/local UoW scope and other raw model changes do not themselves
emit this security feed. Where they invalidate buffer evidence, explicitly request
policy reconciliation or handle the state through the host; no all-model observer
or automatic provider re-query is implied.

[HostBehaviorTests](../Forms.Tests/HostBehaviorTests.cs) adds 31 cases for default
facade/direct-helper notifications, worker clearing with preserved dirty records,
field masking/visibility/editability without query, burst/reentrant revisions,
accepted-query-before-dispatch, stale owners, partial clearing, no-notification
state, coherent/bounded feeds, close/drain, explicit reconciliation/cancellation,
early-dispatch acknowledgement and shared/legacy helper ownership.
See [qualification evidence](IMPLEMENTATION-LOG.md). E-01 through E-09 remain;
the UI checklist adds E-10 for policy revocation/reconciliation on real adapters.

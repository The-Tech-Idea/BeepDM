# Permission Projection

Scope: FormsManager and platform-neutral integration, not native controls.

## Configuration And Effective Permission

DataBlockInfo QueryAllowed/InsertAllowed/UpdateAllowed/DeleteAllowed and ItemInfo
Enabled/Visible retain their existing public property signatures. Setters now
write authored configuration only. Getters return configuration AND the current
runtime security grant. Setting true under a denial cannot lift that denial;
setting false while the effective value is already false still records an authored
restriction. Clearing a rule or granting admin access removes only the policy
restriction, not an application restriction.

The immutable runtime overlay is internal, registration-owned and revisioned.
Another owner or older revision cannot overwrite it. Do not reuse live ItemInfo
instances across registrations. ItemInfo.Clone copies authored Enabled/Visible,
not runtime denial; it remains a definition clone, not a security-authorized row.
Other item properties retain their existing semantics, including key-field DML
restrictions. There is no general configuration snapshot or graph transaction.

Existing serializers see effective public flags, not a new configuration field
or runtime overlay. Serializing a live denied DTO is therefore not authored-form
persistence. System.Text.Json still needs an application policy for existing
System.Type metadata. Preserve the host's explicit definition persistence path.

## Default Publication

On policy changes, FormsManager captures all live registrations, not just policy
map entries. This restores policy grants when ClearBlockSecurity removes entries.
Policies installed before registration are projected after registration publication
and before BlockEnter observers. Replacement gets fresh generated definitions and
the current policy; it does not copy a retired definition's runtime overlay.

Permission and field-policy evaluation and metadata capture occur outside ownership
monitors. Default ItemPropertyManager implements optional IItemSecurityProjection:
it copies descriptors, checks the exact registry and item references, then invokes
the synchronously fenced, same-thread publication action. A failed attempt also
consumes that action; it cannot be retried in the authorization window. Default lock order is
item registry -> security revision -> manager registration. The action only writes
owned model memory; it must not invoke providers, triggers, observers, setters on
foreign objects, or metadata getters.

After monitors are released, effective Enabled/Visible changes notify observers.
Each observer failure is retained without undoing permissions or suppressing other
current observers. Registry/item identity and manager registration/policy freshness
are rechecked between deliveries; replacement or reentrant newer policy stops the
remaining old notifications. Manager PolicyNotificationFailures keeps bounded
failure evidence. Typed and generic authored Enabled/Visible setters emit changes
only when effective permission changes; general helper event failure behavior is
not otherwise redesigned.

Publication is not an atomic transaction across every block, item and UI surface.
An exception can follow partial model publication. Registration publication and
permission projection are separate windows. Managed read/write policy enforcement
remains independently required; public flags are not an authorization credential.
No claim covers an arbitrary concurrent check-to-use window or raw model edits.

## Injected Helpers And UI

IItemPropertyManager and ISecurityManager have no mandatory interface additions.
Custom item helpers can opt into IItemSecurityProjection and must qualify its
registry, owned-memory and observer contracts. Legacy helpers use captured-model
projection without registry gating or helper projection notifications. Manager
projection evaluates the required IsBlockAllowed/GetFieldSecurity APIs rather
than invoking bulk ApplyBlockSecurityFlags/ApplyFieldSecurityFlags callbacks;
custom implementations must make those APIs authoritative and consistent.
Custom security without IQuerySecurityPublication has a weaker revision check,
not the default atomic policy-publication gate. Missing policy feeds still have
the compatibility limits in [Policy Repaint](POLICY-REPAINT.md).

Replacing/adding an item directly through a raw helper is not a policy publication
and does not auto-project its flags. Raw helper/model operations, property classes,
general event delivery, cross-form sharing and whole-definition persistence remain
separate qualification boundaries.

Clearing a rule advances read authorization. Restored QueryAllowed does not certify
old rows: [buffer authorization](BUFFER-AUTHORIZATION.md) still requires an accepted
managed read before rendering. Use the binding's deliberate refresh/reconciliation
and inspect acknowledgement. [Queued clearing](POLICY-REPAINT.md) is not immediate
privacy; hide/lock before switching principals. Native adapters remain unqualified.

## Evidence And Remaining Gates

[PermissionProjectionTests](../Forms.Tests/PermissionProjectionTests.cs) and the
additional [host regression](../Forms.Tests/HostBehaviorTests.cs) cover clearing,
configuration before/during denial, typed/generic/direct writes, clone/serializer
boundaries, replacement, observer faults/reentrancy/registry retirement, stale/late/
foreign-thread publication, owner/revision fencing and preinstalled policy.
Read [Implementation Log](IMPLEMENTATION-LOG.md) for source-built results and
initial qualification failures. All authoritative stages A-G remain open.
Whole-definition/configuration pinning, concurrent raw edits, helper mutation feeds,
auxiliary policies, native UI E-01 through E-10 and earlier release instability
are not closed by this increment.

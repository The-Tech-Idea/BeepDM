# Cached Buffer Authorization For UI Targets

Status: initial Stage B/D/E binding boundary, not authorization of raw data access,
arbitrary row mutations, every Forms operation or external adapters.

## Publication Evidence

[BufferAuthorization](FormsManager.BufferAuthorization.cs) records the security
helper identity, read-authorization revision, provider/entity/schema/default clause and opaque
buffer identity for a registration. [Managed publication](FormsManager.ManagedReadPolicy.cs)
installs the receipt in the existing security/registration publication window,
after the owned row swap and before PostQuery observers. Preparation, failed
publication and legacy Get never certify a new buffer. A failed re-query can keep
an earlier receipt only while its original policy and buffer remain current.

The optional [buffer capabilities](../../../DataManagementModelsStandard/Editor/IUnitofWorkReadBufferIdentity.cs)
do not add mandatory members to IUnitofWork or its read stage. The default generic
UoW and wrapper implement them. The stage exposes only an opaque prepared identity,
not candidate rows; its successful publication installs that identity. Replacement,
Clear, filtered-buffer assignment and ScopeToTenant invalidate it, including observed
ABA changes. Cursor movement, field edits and commit tracking acceptance do not
require a provider re-query. Disposed wrappers retain capability support identity
but cannot return a live receipt. Custom readers need both optional capabilities
and their own qualification; inherited/default semantics are not adapter certification.

[QuerySecuritySnapshot](../../../DataManagementModelsStandard/Editor/Forms/Interfaces/IQuerySecuritySnapshotProvider.cs)
now has ReadAuthorizationRevision. The original four-argument constructor remains
and conservatively uses Revision for both values. Default SecurityManager advances
both revisions on every context/block-policy change or clear, even equivalent/ABA
changes. Field-only SetFieldSecurity advances the full revision, not the read
revision: old delivery tokens expire, but a fresh token can remask the same authorized
buffer. Block-policy changes conservatively require re-query even for another block
or a DML-only rule; no per-block policy dependency optimizer is claimed.

## Binding And Compatibility

[Binding target capture](FormsManager.BindingTargets.cs) checks the receipt before
reading record fields. Record-aware delivery/edit/focus checks require it again,
as well as their existing registration/record/query/full-security revisions.
A fresh token cannot bless rows retained from an older principal, roles, claims,
row filter or denied-query policy. A successful managed basic/enhanced/typed query
or detail read restores capture under the new policy. Re-registering a held buffer
under that configured policy does not certify it. Registration-only checks remain
lifetime checks, not record authorization.

The original default unscoped preloaded buffer has a narrow compatibility receipt
at registration, only before any default context/block policy has been set. This
also requires the optional UoW's RequiresReadAuthorization to remain false; a local
tenant read scope cannot bootstrap trust by re-registering its old rows. This
is trusted caller data, not proof that a preloaded row set obeys a tenant policy.
Legacy sources without buffer identity retain weaker collection-reference checks
only in this compatibility case. Scoped/new-principal bindings, including initial
attach, require acknowledged managed publication; Clear alone does not authorize
an empty buffer or restore a revoked token. Hosts must handle capture failure and
complete a managed read before attaching/rendering that context.

No user getter, observer, presenter or provider is invoked under the receipt's
registration monitor. The pure receipt pointer is published alongside owned query
state. This is observed cooperative ownership, not atomic protection against every
raw concurrent setter or a dishonest optional capability implementation.

## Open Gates

The receipt check alone does not clear displayed text. The opt-in binding now adds
[queued policy reconciliation](POLICY-REPAINT.md): revoked buffers are hidden/cleared,
authorized buffers remasked. Immediate privacy still requires host-side hiding before
principal switch; await physical drain and inspect failures, then managed re-query.
Raw Units/record additions and mutations, nested opaque values, low-level datasource
access, provider correctness, LOV/record-group/cache reads and export/audit routes
remain trusted/unqualified boundaries. The receipt does not evaluate row predicates
in memory or replace the shared filter compiler with a weaker evaluator. Editor/LOV
and other public operations have their own current contracts, not this UI receipt.

[HostBehaviorTests](../Forms.Tests/HostBehaviorTests.cs) adds 30 real-UoW/mock-host
cases for principal/roles/claims/filter/query-denial/ABA revocation, no raw host reads
after rejection, three query routes, field-only masking without provider reads,
buffer/provider/schema replacement, re-registration, notification order, failed
and in-flight reads, managed detail restoration, tenant scope (including in-flight
ABA rejection and scoped initial registration) and disposed wrappers.
See [implementation evidence](IMPLEMENTATION-LOG.md) for the source-built matrix.
